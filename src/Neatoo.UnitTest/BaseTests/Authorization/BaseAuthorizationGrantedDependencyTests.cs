//using Microsoft.Extensions.DependencyInjection;
//using Microsoft.VisualStudio.TestTools.UnitTesting;
//using Neatoo.AuthorizationRules;
//using Neatoo.RemoteFactory;
//using Neatoo.UnitTest.Objects;
//using System;
//using System.Threading.Tasks;

//namespace Neatoo.UnitTest.BaseTests.Authorization;


//public interface IAuthorizationGrantedDependencyRule : IAuthorizationRule
//{
//    int Criteria { get; set; }
//    bool ExecuteCreateCalled { get; }
//    bool ExecuteFetchCalled { get; set; }
//    bool ExecuteUpdateCalled { get; set; }
//    bool ExecuteDeleteCalled { get; set; }
//}

//public class AuthorizationGrantedDependencyRule : AuthorizationRule, IAuthorizationGrantedDependencyRule
//{
//    public int Criteria { get; set; }
//    public bool ExecuteCreateCalled { get; set; }
//    private IDisposableDependency DisposableDependency { get; }

//    public AuthorizationGrantedDependencyRule(IDisposableDependency disposableDependency)
//    {
//        DisposableDependency = disposableDependency;
//    }

//    [Execute(AuthorizeOperation.Create)]
//    public AuthorizationRuleResult ExecuteCreate()
//    {
//        Assert.IsNotNull(DisposableDependency);
//        ExecuteCreateCalled = true;
//        return AuthorizationRuleResult.AccessGranted();
//    }

//    [Execute(AuthorizeOperation.Create)]
//    public AuthorizationRuleResult ExecuteCreate(int criteria)
//    {
//        Assert.IsNotNull(DisposableDependency);
//        ExecuteCreateCalled = true;
//        Criteria = criteria;
//        return AuthorizationRuleResult.AccessGranted();
//    }

//    public bool ExecuteFetchCalled { get; set; }
//    [Execute(AuthorizeOperation.Fetch)]
//    public AuthorizationRuleResult ExecuteFetch()
//    {
//        Assert.IsNotNull(DisposableDependency);
//        ExecuteFetchCalled = true;
//        return AuthorizationRuleResult.AccessGranted();
//    }

//    [Execute(AuthorizeOperation.Fetch)]
//    public AuthorizationRuleResult ExecuteFetch(int criteria)
//    {
//        Assert.IsNotNull(DisposableDependency);
//        ExecuteFetchCalled = true;
//        Criteria = criteria;
//        return AuthorizationRuleResult.AccessGranted();
//    }

//    public bool ExecuteUpdateCalled { get; set; }
//    [Execute(AuthorizeOperation.Update)]
//    public AuthorizationRuleResult ExecuteUpdate()
//    {
//        Assert.IsNotNull(DisposableDependency);
//        ExecuteUpdateCalled = true;
//        return AuthorizationRuleResult.AccessGranted();
//    }

//    public bool ExecuteDeleteCalled { get; set; }
//    [Execute(AuthorizeOperation.Delete)]
//    public AuthorizationRuleResult ExecuteDelete()
//    {
//        Assert.IsNotNull(DisposableDependency);
//        ExecuteDeleteCalled = true;
//        return AuthorizationRuleResult.AccessGranted();
//    }
//}

//public interface IBaseAuthorizationGrantedDependencyObject : IBase { }

//public class BaseAuthorizationGrantedDependencyObject : Base<BaseAuthorizationGrantedDependencyObject>, IBaseAuthorizationGrantedDependencyObject
//{

//    public BaseAuthorizationGrantedDependencyObject(IBaseServices<BaseAuthorizationGrantedDependencyObject> services) : base(services)
//    {

//    }

//    [AuthorizationRules]
//    public static void RegisterAuthorizationRules(IAuthorizationRuleManager authorizationRuleManager)
//    {
//        authorizationRuleManager.AddRule<IAuthorizationGrantedDependencyRule>();
//    }

//    [Create]
//    public void Create(int criteria) { }

//    [Fetch]
//    public void Fetch() { }

//    [Fetch]
//    public void Fetch(int criteria) { }

//}

//[TestClass]
//public class BaseAuthorizationGrantedDependencyTests
//{

//    IServiceScope scope;
//    INeatooPortal<IBaseAuthorizationGrantedDependencyObject> portal;

//    [TestInitialize]
//    public void TestInitialize()
//    {
//        scope = UnitTestServices.GetLifetimeScope(true);
//        portal = scope.GetRequiredService<INeatooPortal<IBaseAuthorizationGrantedDependencyObject>>();
//    }

//    [TestMethod]
//    public async Task BaseAuthorization_Create()
//    {
//        var obj = await portal.Create();
//        var authRule = scope.GetRequiredService<IAuthorizationGrantedDependencyRule>();
//        Assert.IsTrue(authRule.ExecuteCreateCalled);
//    }

//    [TestMethod]
//    public async Task BaseAuthorization_Create_Criteria()
//    {
//        var criteria = DateTime.Now.Millisecond;
//        var obj = await portal.Create(criteria);
//        var authRule = scope.GetRequiredService<IAuthorizationGrantedDependencyRule>();
//        Assert.IsTrue(authRule.ExecuteCreateCalled);
//        Assert.AreEqual(criteria, authRule.Criteria);
//    }

//    [TestMethod]
//    public async Task BaseAuthorization_Fetch()
//    {
//        var obj = await portal.Fetch();
//        var authRule = scope.GetRequiredService<IAuthorizationGrantedDependencyRule>();
//        Assert.IsTrue(authRule.ExecuteFetchCalled);
//    }

//    [TestMethod]
//    public async Task BaseAuthorization_Fetch_Criteria()
//    {
//        var criteria = DateTime.Now.Millisecond;
//        var obj = await portal.Fetch(criteria);
//        var authRule = scope.GetRequiredService<IAuthorizationGrantedDependencyRule>();
//        Assert.IsTrue(authRule.ExecuteFetchCalled);
//        Assert.AreEqual(criteria, authRule.Criteria);
//    }
//}
