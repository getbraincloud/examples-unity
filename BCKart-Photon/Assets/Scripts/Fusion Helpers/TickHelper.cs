using Fusion;

public static class TickHelper {
    public static float TickToSeconds(NetworkRunner runner, int ticks) {
        return ticks * runner.DeltaTime;
    }
}