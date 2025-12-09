using AuthService.Clients.LdapClient;
using AuthService.Tests.Helpers.Ldap;
using FluentAssertions;
using Novell.Directory.Ldap;

namespace AuthService.Tests.Clients.LdapClient;

public class LdapClientTests
{
    [Fact]
    public async Task AuthenticateAsync_Should_ReturnDomainNotAllowed_When_DomainInvalid()
    {
        // Arrange
        var config = FakeLdapConfig.Create();
        var client = new AuthService.Clients.LdapClient.LdapClient(config);

        var passport = new UserPassport("john", "corp.com", "123");

        // Act
        var result = await client.AuthenticateAsync(passport);

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Be(LdapError.DomainNotAllowed);
    }

    [Fact]
    public async Task AuthenticateAsync_Should_UseTechnicalUserCredentials()
    {
        // Arrange
        LdapConfig config = FakeLdapConfig.Create();
        var fakeConnection = new FakeLdapConnection();
        Func<ILdapConnection> connectionFactory = () => fakeConnection;

        var client = new AuthService.Clients.LdapClient.LdapClient(config, connectionFactory);

        var passport = new UserPassport("john", "reconext.com", "123");

        // Act
        await client.AuthenticateAsync(passport);

        // Assert
        fakeConnection.UsedHost.Should().Be("reconext.com");
        fakeConnection.UsedBindDn.Should().Be("BYD-Intranet@reconext.com");
        fakeConnection.UsedBindPassword.Should().Be("secret");
        fakeConnection.UsedSearchFilter.Should().Be("(sAMAccountName=john)");
    }

    [Fact]
    public async Task AuthenticateAsync_Should_ReturnUserNotFound_When_No_Results()
    {
        // Arrange
        LdapConfig config = FakeLdapConfig.Create();
        var fakeConnection = new FakeLdapConnection();
        Func<ILdapConnection> factory = () => fakeConnection;

        var client = new AuthService.Clients.LdapClient.LdapClient(config, factory);

        var passport = new UserPassport("john", "reconext.com", "123");

        // Act
        var result = await client.AuthenticateAsync(passport);

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Be(LdapError.UserNotFound);
    }

    [Fact]
    public async Task AuthenticateAsync_Should_ReturnOfficeNotAllowed_When_Office_IsInvalid()
    {
        // Arrange
        LdapConfig config = FakeLdapConfig.Create();
        var fakeConnection = new FakeLdapConnection();

        // Create an LDAP entry with an INVALID office
        var attributes = new LdapAttributeSet
        {
            new LdapAttribute("physicalDeliveryOfficeName", "INVALID OFFICE"),
        };

        fakeConnection.SearchEntry = new LdapEntry("cn=john,dc=reconext,dc=com", attributes);

        Func<ILdapConnection> factory = () => fakeConnection;
        var client = new AuthService.Clients.LdapClient.LdapClient(config, factory);

        var passport = new UserPassport("john", "reconext.com", "123");

        // Act
        var result = await client.AuthenticateAsync(passport);

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Be(LdapError.OfficeNotAllowed);
    }

    [Fact]
    public async Task AuthenticateAsync_Should_AuthenticateSuccessfully_When_Office_IsValid()
    {
        // Arrange
        LdapConfig config = FakeLdapConfig.Create();
        var fakeConnection = new FakeLdapConnection();

        // Create an LDAP entry with a VALID office
        var attributes = new LdapAttributeSet
        {
            new LdapAttribute("physicalDeliveryOfficeName", "Havant Site (UK)"),
        };

        fakeConnection.SearchEntry = new LdapEntry("cn=john,dc=reconext,dc=com", attributes);

        Func<ILdapConnection> factory = () => fakeConnection;
        var client = new AuthService.Clients.LdapClient.LdapClient(config, factory);

        var passport = new UserPassport("john", "reconext.com", "123");

        // Act
        var result = await client.AuthenticateAsync(passport);

        // Assert
        result.Success.Should().BeTrue();
        result.Error.Should().BeNull();
        result.User.Should().NotBeNull();
        result.User!.OfficeLocation.Should().Be("Havant Site (UK)");
    }

    [Fact]
    public async Task AuthenticateAsync_Should_ReturnInvalidCredentials_When_UserPasswordIsWrong()
    {
        // Arrange
        LdapConfig config = FakeLdapConfig.Create();
        var fakeConnection = new FakeLdapConnection();

        // User exists & office valid
        var attributes = new LdapAttributeSet
        {
            new LdapAttribute("physicalDeliveryOfficeName", "Havant Site (UK)"),
        };
        fakeConnection.SearchEntry = new LdapEntry("cn=john,dc=reconext,dc=com", attributes);

        // Make user bind fail
        fakeConnection.ThrowInvalidCredentialsOnUserBind = true;

        Func<ILdapConnection> factory = () => fakeConnection;
        var client = new AuthService.Clients.LdapClient.LdapClient(config, factory);

        var passport = new UserPassport("john", "reconext.com", "bad-password");

        // Act
        var result = await client.AuthenticateAsync(passport);

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Be(LdapError.InvalidCredentials);
    }
}
