using System.Security.Cryptography;

namespace GasMapocho.Api.Services;

/// <summary>
/// Verificacion de contrasenas con PBKDF2-SHA256.
///
/// Los parametros deben coincidir EXACTAMENTE con los usados para generar los
/// hash de db/03_seed.sql; si cambian, nadie puede iniciar sesion.
/// </summary>
public static class Seguridad
{
    private const int Iteraciones = 100_000;
    private const int TamanoHash = 32;
    private const int TamanoSal = 32;

    public static byte[] GenerarSal() => RandomNumberGenerator.GetBytes(TamanoSal);

    public static byte[] Hash(string password, byte[] sal) =>
        Rfc2898DeriveBytes.Pbkdf2(password, sal, Iteraciones, HashAlgorithmName.SHA256, TamanoHash);

    public static bool Verificar(string password, byte[] hashGuardado, byte[] sal)
    {
        if (hashGuardado.Length == 0 || sal.Length == 0) return false;

        // Comparacion en tiempo fijo: con == se podria deducir el hash midiendo
        // cuanto tarda en responder.
        return CryptographicOperations.FixedTimeEquals(Hash(password, sal), hashGuardado);
    }
}
