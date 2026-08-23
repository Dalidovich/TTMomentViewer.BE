using System.Security.Cryptography;
using System.Text;

namespace TTMomentViewer.BLL.Helpers;

public static class IdHasher
{
    private const int IdLength = 16;

    public static string HashFolderName(string folderName) => Hash(folderName);

    public static string HashRelativePath(string relativePath) => Hash(NormalizeRelativePath(relativePath));

    public static string NormalizeRelativePath(string relativePath) =>
        relativePath.Replace('\\', '/').ToLowerInvariant();

    private static string Hash(string value)
    {
        var hash = SHA1.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(hash)[..IdLength].ToLowerInvariant();
    }
}
