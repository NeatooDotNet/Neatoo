using Neatoo.Rules;

namespace HorseBarn.lib.Cart;

internal interface ICartNumberOfHorsesRule : IRule<ICart>
{
}

internal class CartNumberOfHorsesRule : RuleBase<ICart>, ICartNumberOfHorsesRule
{
    public CartNumberOfHorsesRule()
    {
        AddTriggerProperties(_ => _.NumberOfHorses, _ => _.HorseList.Count);
    }
    public override PropertyErrors Execute(ICart cart)
    {
        var horseCount = cart.Horses.Count();
        if (horseCount > cart.NumberOfHorses)
        {
            cart.NumberOfHorses = horseCount;
        }
        else if (cart.NumberOfHorses == 0)
        {
            cart.NumberOfHorses = 1;
        } else if(cart.NumberOfHorses > 6)
        {
            cart.NumberOfHorses = 6;
        }
        else if (horseCount != 0 && cart.NumberOfHorses != horseCount)
        {
            return nameof(ICart.NumberOfHorses).PropertyError($"There are {horseCount} but there need to be {cart.NumberOfHorses}");
        }

        return PropertyErrors.None;
    }
}
