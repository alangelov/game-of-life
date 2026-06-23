namespace GameOfLife.Core;

public static class ConwayRules
{
    public static bool NextState(bool isAlive, int neighborCount) =>
        isAlive
            ? neighborCount is 2 or 3
            : neighborCount == 3;
}
