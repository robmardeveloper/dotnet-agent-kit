using ArchUnitNET.Domain;
using ArchUnitNET.Fluent;
using ArchUnitNET.Loader;
using ArchUnitNET.xUnitV3;
using CourtBookings.Application.Bookings;
using CourtBookings.Domain.Bookings;
using Xunit;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace CourtBookings.ArchitectureTests;

// ARCH-4: the dependency rule (ARCH-1, ARCH-2) is checked by these tests, so a forbidden reference fails `dotnet test`.
public class LayerDependencyTests
{
    // Each layer is found through one of its own types: renaming a project then breaks the build
    // here, instead of silently leaving a layer empty.
    private static readonly System.Reflection.Assembly DomainAssembly = typeof(Booking).Assembly;
    private static readonly System.Reflection.Assembly ApplicationAssembly = typeof(BookCourtHandler).Assembly;
    private static readonly System.Reflection.Assembly InfrastructureAssembly = typeof(CourtBookings.Infrastructure.DependencyInjection).Assembly;
    private static readonly System.Reflection.Assembly ApiAssembly = typeof(Program).Assembly;

    private static readonly Architecture Architecture = new ArchLoader()
        .LoadAssemblies(DomainAssembly, ApplicationAssembly, InfrastructureAssembly, ApiAssembly)
        .Build();

    private static readonly IObjectProvider<IType> Domain =
        Types().That().ResideInAssembly(DomainAssembly).As("Domain");

    private static readonly IObjectProvider<IType> Application =
        Types().That().ResideInAssembly(ApplicationAssembly).As("Application");

    private static readonly IObjectProvider<IType> Infrastructure =
        Types().That().ResideInAssembly(InfrastructureAssembly).As("Infrastructure");

    private static readonly IObjectProvider<IType> Api =
        Types().That().ResideInAssembly(ApiAssembly).As("Api");

    [Fact]
    public void Domain_DependsOnOtherLayers_IsForbidden() =>
        Types().That().Are(Domain)
            .Should().NotDependOnAny(Application)
            .AndShould().NotDependOnAny(Infrastructure)
            .AndShould().NotDependOnAny(Api)
            .Check(Architecture);

    [Fact]
    public void Domain_DependsOnFrameworks_IsForbidden() =>
        Types().That().Are(Domain)
            .Should().NotDependOnAnyTypesThat()
            .ResideInNamespaceMatching(@"^Microsoft\.(EntityFrameworkCore|AspNetCore)(\..*)?$")
            .Check(Architecture);

    [Fact]
    public void Application_DependsOnOuterLayers_IsForbidden() =>
        Types().That().Are(Application)
            .Should().NotDependOnAny(Infrastructure)
            .AndShould().NotDependOnAny(Api)
            .Check(Architecture);

    [Fact]
    public void Infrastructure_DependsOnApi_IsForbidden() =>
        Types().That().Are(Infrastructure)
            .Should().NotDependOnAny(Api)
            .Check(Architecture);
}
