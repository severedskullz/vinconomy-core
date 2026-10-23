using System.Reflection;
using Commercially.Common.Interfaces;
using Commercially.Vinconomy.BlockEntityBehaviors.InventoryProviders;
using Commercially.Vinconomy.Config;
using Commercially.Vinconomy.Interfaces;
using Commercially.Vinconomy.Inventory;
using Commercially.Vinconomy.Inventory.Impl;
using Commercially.Vinconomy.Inventory.StallSlots;
using Commercially.Vinconomy.ModSystems;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

int checks = 0, failed = 0;
void Equal(string name, float expected, float actual)
{
    checks++;
    if (Math.Abs(expected - actual) <= 0.00001f) return;
    failed++;
    Console.Error.WriteLine($"FAIL {name}: expected {expected}, got {actual}");
}

// Real Vinconomy inventory and provider methods plus the game's actual cooked-pot
// and bowl dummy-slot callbacks. Only world services, ownership and room climate
// are fixtures; no transition or spoilage calculation is reimplemented here.
foreach (bool bowl in new[] { false, true })
foreach (float climate in new[] { 0.1f, 1f, 2.4f })
foreach (float rate in new[] { 0f, 0.015f, 0.15f, 1f })
foreach (bool admin in new[] { false, true })
foreach (bool decay in new[] { false, true })
{
    var f = new Fixture(climate, rate, admin, decay);
    var container = bowl ? (BlockContainer)new TestBowl(f.Api) : new TestPot(f.Api);
    ItemStack ingredient = f.NewIngredient();
    var parent = new ItemStack(container, 7);
    container.SetContents(parent, new[] { ingredient });
    f.Inventory.Stall.MealSlot.Itemstack = parent;
    ItemSlot inner = bowl
        ? ((TestBowl)container).ContentSlot(f.Inventory.Stall.MealSlot, ingredient)
        : ((TestPot)container).ContentSlot(f.Inventory.Stall.MealSlot, ingredient);
    float expected = admin || !decay ? 0 : rate * climate;
    string label = $"{(bowl ? "bowl" : "pot")}, climate={climate}, rate={rate}, admin={admin}, decay={decay}";
    Equal($"nested rate: {label}", expected,
        inner.Inventory.GetTransitionSpeedMul(EnumTransitionType.Perish, ingredient));
    Equal($"incoming multiplier applied once: {label}", expected * 0.5f,
        f.Inventory.InvokeTransitionSpeedDelegates(EnumTransitionType.Perish, ingredient, 0.5f));

    // Advance a real ingredient through 24 game hours, not just a helper formula.
    TransitionState state = ingredient.Collectible.UpdateAndGetTransitionStates(f.World, inner)[0];
    Equal($"24h spoilage: {label}", 24 * expected, state.TransitionedHours);
    Equal($"serving count unchanged: {label}", 7, parent.StackSize);
    Equal($"ingredient count unchanged: {label}", 1, ingredient.StackSize);
    Equal($"non-perish container contents remain blocked: {label}", 0,
        inner.Inventory.GetTransitionSpeedMul(EnumTransitionType.Dry, ingredient));
}

// Preview/currency templates must remain frozen, including their own ingredients.
{
    var f = new Fixture(1, 0.015f, false, true);
    var pot = new TestPot(f.Api);
    foreach (ItemSlot slot in new ItemSlot[] { f.Inventory.Stall.Product, f.Inventory.Stall.Currency })
    {
        var ingredient = f.NewIngredient();
        var parent = new ItemStack(pot);
        pot.SetContents(parent, new[] { ingredient });
        slot.Itemstack = parent;
        var inner = pot.ContentSlot(slot, ingredient);
        Equal("template itself frozen", 0, f.Inventory.GetTransitionSpeedMul(EnumTransitionType.Perish, parent));
        Equal("template ingredient frozen", 0, inner.Inventory.GetTransitionSpeedMul(EnumTransitionType.Perish, ingredient));
        var state = ingredient.Collectible.UpdateAndGetTransitionStates(f.World, inner)[0];
        Equal("template ingredient does not age", 0, state.TransitionedHours);
    }
    var transferredIngredient = f.NewIngredient();
    var transferredPot = new ItemStack(pot);
    pot.SetContents(transferredPot, new[] { transferredIngredient });
    var transferSlot = f.Inventory.GetTransferSlot();
    transferSlot.Itemstack = transferredPot;
    Equal("transfer container remains real food", 0.015f,
        pot.ContentSlot(transferSlot, transferredIngredient).Inventory.GetTransitionSpeedMul(EnumTransitionType.Perish, transferredIngredient));
    Equal("ordinary stock direct rate unchanged", 0.015f,
        f.Inventory.GetTransitionSpeedMul(EnumTransitionType.Perish, f.NewIngredient()));
    Equal("existing non-perish provider behavior unchanged", 0.25f,
        f.Inventory.InvokeTransitionSpeedDelegates(EnumTransitionType.Dry, f.NewIngredient(), 1));

    var generic = new TestGenericInventory(f.Inventory.modSystem, f.Inventory.StallComponent) { Api = f.Api };
    new TestProvider(generic, 0.4f) { Api = f.Api }.Attach();
    var ordinaryFood = f.NewIngredient();
    Equal("generic shop raw food unchanged", 0.015f,
        generic.GetTransitionSpeedMul(EnumTransitionType.Perish, ordinaryFood));
    var stockPot = new ItemStack(pot);
    pot.SetContents(stockPot, new[] { ordinaryFood });
    var stockSlot = generic.Stall.Stock[0];
    stockSlot.Itemstack = stockPot;
    Equal("generic shop container also honors preservation", 0.006f,
        pot.ContentSlot(stockSlot, ordinaryFood).Inventory.GetTransitionSpeedMul(EnumTransitionType.Perish, ordinaryFood));

    // Registers and other non-stall inventories keep their existing behavior.
    var nonStall = new InventoryGeneric(1, "test-register", null) { Api = f.Api };
    new TestProvider(nonStall, 0.4f) { Api = f.Api }.Attach();
    Equal("non-stall provider unchanged", 0.4f,
        nonStall.GetTransitionSpeedMul(EnumTransitionType.Perish, f.NewIngredient()));

    f.Inventory.modSystem.Config = null;
    Equal("missing shop config freezes contained food", 0,
        pot.ContentSlot(transferSlot, transferredIngredient).Inventory.GetTransitionSpeedMul(EnumTransitionType.Perish, transferredIngredient));
}
Console.WriteLine($"{checks - failed}/{checks} checks passed; {failed} failed.");
return failed == 0 ? 0 : 1;

sealed class Fixture
{
    public ICoreAPI Api { get; }
    public IWorldAccessor World { get; }
    public TestInventory Inventory { get; }
    private readonly Item ingredient = new()
    {
        ItemId = 1,
        Code = new AssetLocation("game:test-food"),
        TransitionableProps = new[] { new TransitionableProperties
        {
            Type = EnumTransitionType.Perish,
            FreshHours = NatFloat.createUniform(1000, 0),
            TransitionHours = NatFloat.createUniform(1000, 0)
        }}
    };

    public Fixture(float climate, float rate, bool admin, bool decay)
    {
        var calendar = Stub.Create<IGameCalendar>((m, _) => m.Name switch
        {
            "get_TotalHours" => 24d,
            _ => throw new NotSupportedException(m.Name)
        });
        World = Stub.Create<IWorldAccessor>((m, _) => m.Name switch
        {
            "get_Calendar" => calendar,
            "get_Rand" => new Random(42),
            "get_Side" => EnumAppSide.Server,
            "get_Config" => new TreeAttribute(),
            _ => throw new NotSupportedException(m.Name)
        });
        var registry = Stub.Create<IClassRegistryAPI>((m, _) => m.Name switch
        {
            "CreateInvNetworkUtil" => null,
            _ => throw new NotSupportedException(m.Name)
        });
        Api = Stub.Create<ICoreAPI>((m, _) => m.Name switch
        {
            "get_World" => World,
            "get_ClassRegistry" => registry,
            "get_Side" => EnumAppSide.Server,
            _ => throw new NotSupportedException(m.Name)
        });
        var owner = Stub.Create<IOwnableChild>((m, _) => m.Name switch
        {
            "get_IsAdminOwned" => admin,
            _ => throw new NotSupportedException(m.Name)
        });
        var component = Stub.Create<IStallComponent>((m, _) => m.Name switch
        {
            "get_Ownable" => owner,
            _ => throw new NotSupportedException(m.Name)
        });
        var system = new VinconomyCoreSystem
        {
            Config = new VinconomyConfig { FoodDecaysInShops = decay, StallPerishRate = rate }
        };
        Inventory = new TestInventory(system, component) { Api = Api };
        var provider = new TestProvider(Inventory, climate) { Api = Api };
        provider.Attach();
    }

    public ItemStack NewIngredient()
    {
        var stack = new ItemStack(ingredient);
        var state = new TreeAttribute();
        state.SetDouble("createdTotalHours", 0);
        state.SetDouble("lastUpdatedTotalHours", 0);
        state["freshHours"] = new FloatArrayAttribute(new[] { 1000f });
        state["transitionHours"] = new FloatArrayAttribute(new[] { 1000f });
        state["transitionedHours"] = new FloatArrayAttribute(new[] { 0f });
        stack.Attributes["transitionstate"] = state;
        return stack;
    }
}

sealed class TestInventory : MealShopInventory
{
    public MealStallSlot Stall => (MealStallSlot)StallSlots[0];
    public TestInventory(VinconomyCoreSystem system, IStallComponent component) : base(new TestEntity(), null)
    {
        modSystem = system;
        StallComponent = component;
        StallSlots = new BaseStallSlot[] { new MealStallSlot(this, 0) };
    }
}
sealed class TestEntity : BlockEntity { }
sealed class TestGenericInventory : GenericShopInventory
{
    public GenericStallSlot Stall => (GenericStallSlot)StallSlots[0];
    public TestGenericInventory(VinconomyCoreSystem system, IStallComponent component) : base(new TestEntity(), null)
    {
        modSystem = system;
        StallComponent = component;
        var stall = new GenericStallSlot(this, 0) { Stock = new[] { new ItemSlot(this) } };
        StallSlots = new BaseStallSlot[] { stall };
    }
}
sealed class TestProvider : BaseInventoryProvider
{
    private readonly InventoryBase inventory;
    private readonly float climate;
    public override InventoryBase Inventory => inventory;
    public TestProvider(InventoryBase inventory, float climate) : base(new TestEntity())
    {
        this.inventory = inventory;
        this.climate = climate;
    }
    public void Attach() => Inventory.OnAcquireTransitionSpeed += Inventory_OnAcquireTransitionSpeed;
    public override float GetPerishRate() => climate;
}
sealed class TestPot : BlockCookedContainerBase
{
    public TestPot(ICoreAPI api) { this.api = api; BlockId = 2; Code = new AssetLocation("game:test-pot"); }
    public ItemSlot ContentSlot(ItemSlot parent, ItemStack child) => GetContentInDummySlot(parent, child);
}
sealed class TestBowl : BlockMeal
{
    public TestBowl(ICoreAPI api) { this.api = api; BlockId = 3; Code = new AssetLocation("game:test-bowl"); }
    public ItemSlot ContentSlot(ItemSlot parent, ItemStack child) => GetContentInDummySlot(parent, child);
}
public class Stub : DispatchProxy
{
    private System.Func<MethodInfo, object?[]?, object?> handler = null!;
    public static T Create<T>(System.Func<MethodInfo, object?[]?, object?> handler) where T : class
    {
        T proxy = Create<T, Stub>();
        ((Stub)(object)proxy).handler = handler;
        return proxy;
    }
    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) => handler(targetMethod!, args);
}
