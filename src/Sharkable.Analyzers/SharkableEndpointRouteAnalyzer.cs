using System.Collections.Immutable;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Sharkable.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class SharkableEndpointRouteAnalyzer : DiagnosticAnalyzer
{
    public const string ConflictId = "SHARK001";

    private static readonly DiagnosticDescriptor ConflictRule = new(
        ConflictId,
        "Duplicate Sharkable endpoint route",
        "Route conflict: '{0}' is already mapped by '{1}'",
        "Sharkable",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        "Two ISharkEndpoint implementations register the same HTTP method + route combination.");

    private static readonly HashSet<string> MapMethods = new()
    {
        "MapGet", "MapPost", "MapPut", "MapPatch", "MapDelete"
    };

    private static readonly Regex SuffixPattern = new(
        "(endpoint|service|services|controller|controllers|apicontroller)$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex VersionPattern = new(
        @"V(\d+)",
        RegexOptions.Compiled);

    private static readonly Regex RouteConstraintPattern = new(
        @"\{(\w+):[^}]+}",
        RegexOptions.Compiled);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(ConflictRule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(OnCompilationStart);
    }

    private void OnCompilationStart(CompilationStartAnalysisContext context)
    {
        var routeMap = new Dictionary<string, (string Route, string Owner)>(
            StringComparer.OrdinalIgnoreCase);

        context.RegisterSemanticModelAction(ctx =>
        {
            foreach (var classDecl in ctx.SemanticModel.SyntaxTree
                .GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>())
            {
                var symbol = ctx.SemanticModel.GetDeclaredSymbol(classDecl);
                if (symbol == null || !ImplementsISharkEndpoint(symbol))
                    continue;

                var groupName = DeriveGroupName(symbol.Name);
                if (groupName == null)
                    continue;

                var addRoutesMethod = FindAddRoutesMethod(classDecl);
                if (addRoutesMethod == null)
                    continue;

                foreach (var invocation in addRoutesMethod.DescendantNodes()
                    .OfType<InvocationExpressionSyntax>())
                {
                    CheckRoute(invocation, groupName, symbol.Name, routeMap, ctx);
                }
            }
        });

        context.RegisterCompilationEndAction(_ =>
        {
            routeMap.Clear();
        });
    }

    private static bool ImplementsISharkEndpoint(INamedTypeSymbol symbol)
    {
        foreach (var iface in symbol.AllInterfaces)
        {
            if (iface.Name == "ISharkEndpoint")
                return true;
        }
        return symbol.BaseType != null && ImplementsISharkEndpoint(symbol.BaseType);
    }

    private static MethodDeclarationSyntax? FindAddRoutesMethod(
        ClassDeclarationSyntax classDecl)
    {
        foreach (var member in classDecl.Members)
        {
            if (member is MethodDeclarationSyntax method
                && method.Identifier.Text == "AddRoutes"
                && method.ParameterList.Parameters.Count == 1)
            {
                return method;
            }
        }
        return null;
    }

    private void CheckRoute(
        InvocationExpressionSyntax invocation,
        string groupName,
        string className,
        Dictionary<string, (string Route, string Owner)> routeMap,
        SemanticModelAnalysisContext ctx)
    {
        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
            return;

        var methodName = memberAccess.Name.Identifier.Text;
        if (!MapMethods.Contains(methodName))
            return;

        var httpMethod = methodName.Replace("Map", "");
        var args = invocation.ArgumentList.Arguments;
        string? routeTemplate = null;

        if (args.Count >= 1
            && args[0].Expression is LiteralExpressionSyntax literal
            && literal.IsKind(SyntaxKind.StringLiteralExpression))
        {
            routeTemplate = literal.Token.ValueText;
        }

        if (routeTemplate == null)
            return;

        var normalizedRoute = NormalizeRoute(routeTemplate);
        var fullRoute = $"api/{groupName}/{normalizedRoute}".TrimEnd('/');
        var key = $"{httpMethod}:{fullRoute}";

        if (routeMap.TryGetValue(key, out var existing))
        {
            var diagnostic = Diagnostic.Create(ConflictRule,
                invocation.GetLocation(),
                fullRoute,
                existing.Owner);
            ctx.ReportDiagnostic(diagnostic);
        }
        else
        {
            routeMap[key] = (fullRoute, className);
        }
    }

    internal static string? DeriveGroupName(string className)
    {
        var cleaned = SuffixPattern.Replace(className, "");
        cleaned = VersionPattern.Replace(cleaned, "@$1");

        if (string.IsNullOrWhiteSpace(cleaned))
            return null;

        return char.ToLower(cleaned[0]) + cleaned.Substring(1);
    }

    internal static string NormalizeRoute(string route)
    {
        return RouteConstraintPattern.Replace(route.TrimStart('/'), "{$1}");
    }
}
