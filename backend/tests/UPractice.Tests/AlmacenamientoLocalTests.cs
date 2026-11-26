using System.Text;
using Microsoft.Extensions.Configuration;
using UPractice.Application.Abstracciones;
using UPractice.Infrastructure.Almacenamiento;

namespace UPractice.Tests;

/// <summary>T-08: proveedor local con URL firmadas por HMAC (S3.8).</summary>
public sealed class AlmacenamientoLocalTests : IDisposable
{
    private static readonly byte[] Pdf = Encoding.ASCII.GetBytes("%PDF-1.4\n% prueba T-08\n%%EOF");

    private readonly string carpeta = Path.Combine(Path.GetTempPath(), "upractice-t08-" + Guid.NewGuid().ToString("N"));
    private readonly RelojManual reloj = new(new DateTimeOffset(2026, 11, 21, 15, 0, 0, TimeSpan.Zero));
    private readonly LocalStorage storage;

    public AlmacenamientoLocalTests() => storage = new LocalStorage(Opciones(carpeta), reloj);

    [Fact]
    public async Task Sube_y_descarga_con_la_url_firmada()
    {
        var key = LlavesArchivo.Curriculum(Guid.NewGuid(), Guid.NewGuid());
        await storage.SubirAsync(key, new MemoryStream(Pdf), "application/pdf", default);

        var url = await storage.ObtenerUrlDescargaAsync(key, LlavesArchivo.VigenciaDescarga, "Mi CV.pdf", default);
        var q = Query(url.Url);

        Assert.StartsWith("http://localhost:5294/api/v1/archivos/local?", url.Url);
        Assert.Equal(reloj.GetUtcNow().AddMinutes(5).UtcDateTime, url.ExpiraEn);
        await using var archivo = storage.AbrirSiFirmaValida(q["key"], long.Parse(q["exp"]), q["nombre"], q["sig"]);
        Assert.NotNull(archivo);
        Assert.Equal(Pdf, await LeerAsync(archivo));
    }

    [Fact]
    public async Task Rechaza_firma_alterada_otra_llave_u_otro_nombre()
    {
        var key = LlavesArchivo.Curriculum(Guid.NewGuid(), Guid.NewGuid());
        var otra = LlavesArchivo.Curriculum(Guid.NewGuid(), Guid.NewGuid());
        await storage.SubirAsync(key, new MemoryStream(Pdf), "application/pdf", default);
        await storage.SubirAsync(otra, new MemoryStream(Pdf), "application/pdf", default);
        var q = Query((await storage.ObtenerUrlDescargaAsync(key, TimeSpan.FromMinutes(5), "cv.pdf", default)).Url);
        var exp = long.Parse(q["exp"]);

        Assert.Null(storage.AbrirSiFirmaValida(key, exp, q["nombre"], q["sig"][..^2] + "AA"));
        Assert.Null(storage.AbrirSiFirmaValida(otra, exp, q["nombre"], q["sig"]));      // misma firma, otro archivo
        Assert.Null(storage.AbrirSiFirmaValida(key, exp + 3600, q["nombre"], q["sig"])); // alargar la vigencia
        Assert.Null(storage.AbrirSiFirmaValida(key, exp, "otro.pdf", q["sig"]));
    }

    [Fact]
    public async Task La_url_deja_de_funcionar_al_vencer()
    {
        var key = LlavesArchivo.Curriculum(Guid.NewGuid(), Guid.NewGuid());
        await storage.SubirAsync(key, new MemoryStream(Pdf), "application/pdf", default);
        var q = Query((await storage.ObtenerUrlDescargaAsync(key, TimeSpan.FromMinutes(5), "cv.pdf", default)).Url);

        reloj.Avanzar(TimeSpan.FromMinutes(5).Add(TimeSpan.FromSeconds(1)));

        Assert.Null(storage.AbrirSiFirmaValida(q["key"], long.Parse(q["exp"]), q["nombre"], q["sig"]));
    }

    [Fact]
    public async Task Una_url_firmada_con_otra_clave_no_sirve()
    {
        var key = LlavesArchivo.Curriculum(Guid.NewGuid(), Guid.NewGuid());
        await storage.SubirAsync(key, new MemoryStream(Pdf), "application/pdf", default);
        var otraApi = new LocalStorage(Opciones(carpeta), reloj); // clave aleatoria distinta
        var q = Query((await otraApi.ObtenerUrlDescargaAsync(key, TimeSpan.FromMinutes(5), "cv.pdf", default)).Url);

        Assert.Null(storage.AbrirSiFirmaValida(q["key"], long.Parse(q["exp"]), q["nombre"], q["sig"]));
    }

    [Fact]
    public async Task Subir_otra_vez_reemplaza_y_eliminar_es_idempotente()
    {
        var key = LlavesArchivo.DocumentoPractica(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        await storage.SubirAsync(key, new MemoryStream(Encoding.ASCII.GetBytes("%PDF-viejo")), "application/pdf", default);
        await storage.SubirAsync(key, new MemoryStream(Pdf), "application/pdf", default);
        var ruta = Path.Combine(carpeta, key.Replace('/', Path.DirectorySeparatorChar));

        Assert.Equal(Pdf, await File.ReadAllBytesAsync(ruta));
        Assert.Single(Directory.GetFiles(Path.GetDirectoryName(ruta)!)); // no quedan temporales

        await storage.EliminarAsync(key, default);
        await storage.EliminarAsync(key, default);
        Assert.False(File.Exists(ruta));
    }

    [Theory]
    [InlineData("../fuera.pdf")]
    [InlineData("curriculums/../../fuera.pdf")]
    [InlineData("/etc/passwd")]
    [InlineData("C:/Windows/win.ini")]
    [InlineData("curriculums\\x\\y.pdf")]
    [InlineData("curriculums/sin-extension")]
    [InlineData("")]
    public async Task Rechaza_llaves_peligrosas(string key)
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
            storage.SubirAsync(key, new MemoryStream(Pdf), "application/pdf", default));
        Assert.Null(storage.AbrirSiFirmaValida(key, long.MaxValue, "x.pdf", "firma"));
    }

    [Fact]
    public void Supabase_sin_credenciales_detiene_el_arranque()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Storage:Proveedor"] = "Supabase" })
            .Build();

        var error = Assert.Throws<InvalidOperationException>(() => OpcionesAlmacenamiento.Desde(config));
        Assert.Contains("Storage__SupabaseClave", error.Message);
    }

    internal static OpcionesAlmacenamiento Opciones(string carpeta) => OpcionesAlmacenamiento.Desde(
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Storage:RutaLocal"] = carpeta })
            .Build());

    private static Dictionary<string, string> Query(string url) =>
        new Uri(url).Query.TrimStart('?').Split('&')
            .Select(p => p.Split('=', 2))
            .ToDictionary(p => p[0], p => Uri.UnescapeDataString(p[1]));

    private static async Task<byte[]> LeerAsync(Stream s)
    {
        using var ms = new MemoryStream();
        await s.CopyToAsync(ms);
        return ms.ToArray();
    }

    public void Dispose()
    {
        if (Directory.Exists(carpeta)) Directory.Delete(carpeta, recursive: true);
    }
}

internal sealed class RelojManual(DateTimeOffset inicio) : TimeProvider
{
    private DateTimeOffset ahora = inicio;
    public override DateTimeOffset GetUtcNow() => ahora;
    public void Avanzar(TimeSpan t) => ahora = ahora.Add(t);
}
