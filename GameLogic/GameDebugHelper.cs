
namespace GameLogic;

public static class GameDebugHelper
{
    public static void SetFoodPosition(Game game, (int y, int x) pos)
    {
        game.FoodPosition = pos;
    }
}
