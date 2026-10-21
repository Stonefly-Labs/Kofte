using System.Globalization;
using System.Text.Json;
using Kofte.Platform.Modules.Manifest;

return Run(args);

static int Run(string[] arguments)
{
    var options = new Dictionary<string, string>(StringComparer.Ordinal);
    for (var i = 0; i < arguments.Length; i += 2)
    {
        if (i + 1 >= arguments.Length || arguments[i] is not
            ("--host-contract" or "--previous-id" or "--culture" or "--repeat")
            || !options.TryAdd(arguments[i], arguments[i + 1]))
            return Failure("invocation_invalid", 2);
    }
    if (!options.TryGetValue("--host-contract", out var host))
        return Failure("invocation_invalid", 2);
    var repetitions = 1;
    if (options.TryGetValue("--repeat", out var repeat)
        && (!int.TryParse(repeat, NumberStyles.None, CultureInfo.InvariantCulture, out repetitions)
            || repetitions is < 1 or > 100))
        return Failure("invocation_invalid", 2);
    try
    {
        if (options.TryGetValue("--culture", out var culture))
        {
            if (culture is not ("en-US" or "tr-TR")) return Failure("invocation_invalid", 2);
            CultureInfo.CurrentCulture = new CultureInfo(culture, false);
            CultureInfo.CurrentUICulture = new CultureInfo(culture, false);
            Console.Error.WriteLine("culture_verified:" + CultureInfo.CurrentCulture.Name + ":" +
                1.5m.ToString(CultureInfo.CurrentCulture));
        }
        var bytes = new byte[65537];
        using var input = Console.OpenStandardInput();
        var length = 0;
        while (length < bytes.Length)
        {
            var count = input.Read(bytes, length, bytes.Length - length);
            if (count == 0) break;
            length += count;
        }
        options.TryGetValue("--previous-id", out var prior);
        var context = new ManifestValidationContext(host, prior);
        var exit = 0;
        for (var i = 0; i < repetitions; i++)
        {
            var result = ManifestValidator.Validate(bytes.AsSpan(0, length), context);
            switch (result)
            {
                case ManifestValidationResult.Accepted:
                    Console.WriteLine("{\"status\":\"accepted\"}");
                    break;
                case ManifestValidationResult.Rejected rejected:
                    Console.WriteLine(JsonSerializer.Serialize(new
                    {
                        status = "rejected",
                        diagnostics = rejected.Diagnostics.Select(d => new
                        {
                            code = d.Code,
                            origin = d.Origin,
                            path = d.Path,
                            message = d.Message
                        })
                    }));
                    exit = 2;
                    break;
                default:
                    return Failure("consumer_internal_failure", 1);
            }
        }
        return exit;
    }
    catch (IOException) { return Failure("consumer_io_failure", 1); }
    catch (UnauthorizedAccessException) { return Failure("consumer_io_failure", 1); }
    catch (CultureNotFoundException) { return Failure("consumer_culture_unavailable", 1); }
}

static int Failure(string code, int exit)
{
    Console.Error.WriteLine(code);
    return exit;
}
