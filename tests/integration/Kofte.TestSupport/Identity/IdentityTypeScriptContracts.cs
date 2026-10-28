using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using Kofte.Infrastructure.Storage;

namespace Kofte.TestSupport.Identity;

internal static class IdentityTypeScriptContracts
{
    internal static async Task RunAsync(ProcessStartInfo start, string reportFile, int expectedTests,
        CancellationToken token)
    {
        var result = await OwnedProcesses.RunAsync(start, TimeSpan.FromSeconds(45), token);
        UnixStorePaths.ValidateFile(reportFile);
        if (new FileInfo(reportFile).Length is 0 or > 1_000_000)
            throw new InvalidOperationException("identity_typescript_contract_report_invalid");
        using var report = JsonDocument.Parse(await File.ReadAllBytesAsync(reportFile, token));
        var value = report.RootElement;
        if (result.ExitCode != 0)
        {
            var cases = value.GetProperty("testResults").EnumerateArray()
                .SelectMany(file => file.GetProperty("assertionResults").EnumerateArray())
                .Select((test, index) => (Test: test, Index: index))
                .Where(test => test.Test.GetProperty("status").GetString() == "failed")
                .Select(test =>
                {
                    var message = test.Test.GetProperty("failureMessages").ToString();
                    var lines = Regex.Matches(message, @"(?:identity|adapter)\.test\.ts:([1-9][0-9]{0,3}):")
                        .Select(match => match.Groups[1].Value).Distinct();
                    var boundary = Regex.Match(message,
                        @"identity_independent_(route|header|target)_([0-9]_[0-9]{1,5}_[0-9]{3})");
                    return test.Index.ToString(CultureInfo.InvariantCulture) + "_lines_" + string.Join("_", lines) +
                        (boundary.Success ? "_" + boundary.Groups[1].Value + "_" + boundary.Groups[2].Value : "");
                }).ToArray();
            throw new InvalidOperationException("identity_typescript_contract_failed_" + string.Join("_case_", cases));
        }
        if (!value.GetProperty("success").GetBoolean() ||
            value.GetProperty("numPassedTests").GetInt32() != expectedTests ||
            value.GetProperty("numFailedTests").GetInt32() != 0)
            throw new InvalidOperationException("identity_typescript_contract_incomplete");
    }
}
