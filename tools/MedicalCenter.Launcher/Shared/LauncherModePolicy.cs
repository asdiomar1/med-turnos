namespace MedicalCenter.Launcher.Shared;

public static class LauncherModePolicy
{
    public static readonly int[] PrerequisitePorts = { 5433, 6380, 8090 };

    public static bool RequiresPrerequisiteChecks(LaunchMode mode)
        => mode is LaunchMode.FullDocker
            or LaunchMode.InfraOnly
            or LaunchMode.LocalRun
            or LaunchMode.Rebuild;
}
