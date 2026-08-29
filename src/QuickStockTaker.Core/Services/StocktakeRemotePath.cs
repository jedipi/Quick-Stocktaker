namespace QuickStockTaker.Core.Services
{
    internal static class StocktakeRemotePath
    {
        public static string Build(string folder, string fileName, Func<string, string> encode)
        {
            var isRooted = !string.IsNullOrWhiteSpace(folder) && folder.TrimStart().StartsWith("/");
            var parts = folder?
                .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(encode)
                .ToList() ?? new List<string>();

            parts.Add(encode(fileName));

            var path = string.Join("/", parts);
            return isRooted ? $"/{path}" : path;
        }

        public static IReadOnlyList<string> BuildDirectoryPaths(string remotePath)
        {
            var directoryEnd = remotePath.LastIndexOf('/');
            if (directoryEnd <= 0 && !remotePath.StartsWith("/"))
                return [];

            var directory = directoryEnd == 0
                ? "/"
                : remotePath[..directoryEnd];

            var isRooted = directory.StartsWith("/");
            var parts = directory
                .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            var paths = new List<string>();
            var current = isRooted ? "/" : string.Empty;
            foreach (var part in parts)
            {
                current = current == "/"
                    ? $"/{part}"
                    : string.IsNullOrEmpty(current)
                        ? part
                        : $"{current}/{part}";

                paths.Add(current);
            }

            return paths;
        }
    }
}
