using FYPBidNetra.Controllers;
using FYPBidNetra.Models;
using FYPBidNetra.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Configuration;
using Moq;
using System.Security.Claims;
using Xunit;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Org.BouncyCastle.Tls;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Routing;

namespace FYPBidNetra.Tests.Controllers
{
    public class AccountControllerTests
    {
        private readonly Mock<FypContext> _mockContext;
        private readonly Mock<IDataProtector> _mockProtector;
        private readonly Mock<IWebHostEnvironment> _mockEnvironment;
        private readonly Mock<IDataProtectionProvider> _mockProvider;
        private readonly AccountController _controller;

        public AccountControllerTests()
        {
            // Setup mocks
            _mockContext = new Mock<FypContext>();
            _mockProtector = new Mock<IDataProtector>();
            _mockEnvironment = new Mock<IWebHostEnvironment>();
            _mockProvider = new Mock<IDataProtectionProvider>();

            // Setup data protection
            _mockProvider
 .Setup(x => x.CreateProtector(It.IsAny<string>()))
 .Returns(_mockProtector.Object);

            // Setup controller
            _controller = new AccountController(
 _mockContext.Object,
 new DataSecurityProvider(),
 _mockProvider.Object,
 _mockEnvironment.Object
 );

            // Setup controller context with session and TempData
            var httpContext = new DefaultHttpContext();
            httpContext.Session = new TestSession();
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };

            // 👇 Add TempData to avoid null reference error
            var tempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());
            _controller.TempData = tempData;
        }

        [Fact]
        [Trait("SuccessMessage", "OTP is sent")]
        public async Task Register_ValidUser_ReturnsRedirectToVerifyRegistration()
        {
            // Arrange
            var userListEdit = new UserListEdit
            {
                FirstName = "Sujan",
                MiddleName = "Prasad",
                LastName = "Bajgain",
                EmailAddress = "sujanprasadbajgain123@gmail.com",
                Province = "Koshi",
                District = "Sunsari",
                City = "Itahari",
                Gender = "Male",
                Phone = "9812362340",
                UserRole = "Bidder",
                UserPassword = "Password123!",
                UserPhoto = "Sujan.jpg"
            };

            var users = new List<UserList>().AsQueryable();

            var mockSet = new Mock<DbSet<UserList>>();
            mockSet.As<IQueryable<UserList>>().Setup(m => m.Provider).Returns(users.Provider);
            mockSet.As<IQueryable<UserList>>().Setup(m => m.Expression).Returns(users.Expression);
            mockSet.As<IQueryable<UserList>>().Setup(m => m.ElementType).Returns(users.ElementType);
            mockSet.As<IQueryable<UserList>>().Setup(m => m.GetEnumerator()).Returns(users.GetEnumerator());

            _mockContext.Setup(x => x.UserLists).Returns(mockSet.Object);
            _mockProtector.Setup(x => x.Protect(It.IsAny<byte[]>())).Returns((byte[] b) => b);
            _mockProtector.Setup(x => x.Unprotect(It.IsAny<byte[]>())).Returns((byte[] b) => b);

            // Act
            var result = await _controller.Register(userListEdit) as RedirectToActionResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("VerifyRegistration", result.ActionName);
            Assert.Equal("sujanprasadbajgain123@gmail.com", result.RouteValues["email"]);
        }

        [Fact]
        [Trait("ErrorMessage", "User already exists with this email!")]
        public async Task Register_ExistingEmail_ReturnsViewWithError()
        {
            // Arrange
            var userListEdit = new UserListEdit
            {
                FirstName = "Sujan",
                MiddleName = "Prasad",
                LastName = "Bajgain",
                EmailAddress = "sujanprasadbajgain123@gmail.com",
                Province = "Koshi",
                District = "Sunsari",
                City = "Itahari",
                Gender = "Male",
                Phone = "9812362340",
                UserRole = "Bidder",
                UserPassword = "Password123!",
                UserPhoto = "Hari.jpg"
            };

            var users = new List<UserList>
 {
 new UserList
 {
 FirstName = "Hari",
 MiddleName = "Prasad",
 LastName = "Gelal",
 EmailAddress = "sujanprasadbajgain123@gmail.com",
 Province = "Koshi",
 District = "Sunsari",
 City = "Itahari",
 Gender = "Male",
 Phone = "9812345678",
 UserRole = "Bidder",
 UserPassword = "Password123!",
 UserPhoto = "Hari.jpg"
 }
 }.AsQueryable();

            var mockSet = new Mock<DbSet<UserList>>();
            mockSet.As<IQueryable<UserList>>().Setup(m => m.Provider).Returns(users.Provider);
            mockSet.As<IQueryable<UserList>>().Setup(m => m.Expression).Returns(users.Expression);

