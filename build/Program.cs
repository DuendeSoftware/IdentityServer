using System;
using System.IO;
using static Bullseye.Targets;
using static SimpleExec.Command;

namespace build
{
    internal static class Program
    {
        private const string solution = "Duende.IdentityServer.sln";
        private const string packOutput = "./artifacts";
        private const string envVarMissing = " environment variable is missing. Aborting.";

        private static class Targets
        {
            public const string RestoreTools = "restore-tools";
            public const string CleanBuildOutput = "clean-build-output";
            public const string CleanPackOutput = "clean-pack-output";
            public const string Build = "build";
            public const string Test = "test";
            public const string Pack = "pack";
            public const string SignBinary = "sign-binary";
            public const string SignPackage = "sign-package";
        }

        internal static void Main(string[] args)
        {
            Target(Targets.RestoreTools, () =>
            {
                Run("dotnet", "tool restore");
            });

            Target(Targets.CleanBuildOutput, () =>
            {
                Run("dotnet", $"clean {solution} -c Release -v m --nologo");
            });

            Target(Targets.Build, DependsOn(Targets.CleanBuildOutput), () =>
            {
                Run("dotnet", $"build {solution} -c Release --nologo");
            });

            Target(Targets.SignBinary, DependsOn(Targets.Build, Targets.RestoreTools), () =>
            {
                // sign all dlls
            });

            Target(Targets.Test, DependsOn(Targets.Build), () =>
            {
                Run("dotnet", $"test {solution} -c Release --no-build --nologo");
            });

            Target(Targets.CleanPackOutput, () =>
            {
                if (Directory.Exists(packOutput))
                {
                    Directory.Delete(packOutput, true);
                }
            });

            Target(Targets.Pack, DependsOn(Targets.Build, Targets.CleanPackOutput), () =>
            {
                var directory = Directory.CreateDirectory(packOutput).FullName;
                
                Run("dotnet", $"pack ./src/Storage/Duende.IdentityServer.Storage.csproj -c Release -o {directory} --no-build --nologo");
                Run("dotnet", $"pack ./src/IdentityServer/Duende.IdentityServer.csproj -c Release -o {directory} --no-build --nologo");
                
                Run("dotnet", $"pack ./src/EntityFramework.Storage/Duende.IdentityServer.EntityFramework.Storage.csproj -c Release -o {directory} --no-build --nologo");
                Run("dotnet", $"pack ./src/EntityFramework/Duende.IdentityServer.EntityFramework.csproj -c Release -o {directory} --no-build --nologo");
                
                Run("dotnet", $"pack ./src/AspNetIdentity/Duende.IdentityServer.AspNetIdentity.csproj -c Release -o {directory} --no-build --nologo");
            });

            Target(Targets.SignPackage, DependsOn(Targets.Pack, Targets.RestoreTools), () =>
            {
                Sign(packOutput, "*.nupkg");
            });

            Target("default", DependsOn(Targets.Test, Targets.Pack));

            Target("sign", DependsOn(Targets.SignBinary, Targets.Test, Targets.SignPackage));

            RunTargetsAndExit(args, ex => ex is SimpleExec.NonZeroExitCodeException || ex.Message.EndsWith(envVarMissing));
        }

        private static void Sign(string path, string searchTerm)
        {
            var signClientSecret = Environment.GetEnvironmentVariable("SignClientSecret");

            if (string.IsNullOrWhiteSpace(signClientSecret))
            {
                throw new Exception($"SignClientSecret{envVarMissing}");
            }

            foreach (var file in Directory.GetFiles(path, searchTerm, SearchOption.AllDirectories))
            {
                Console.WriteLine($"  Signing {file}");
                Run("dotnet", $"SignClient sign -c signClient.json -i {file} -r sc-ids@dotnetfoundation.org -s \"{signClientSecret}\" -n 'IdentityServer4'", noEcho: true);
            }
        }
    }
}