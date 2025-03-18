//using Microsoft.Extensions.DependencyInjection;
//using System;
//using System.Collections.Concurrent;
//using System.Collections.Generic;
//using System.Linq;
//using System.Reflection;
//using System.Threading.Tasks;

//namespace Neatoo.AuthorizationRules;


//public interface IAuthorizationRuleManager
//{
//    bool IsRegistered { get; }
//    void AddRule<R>() where R : IAuthorizationRule;
//    Task CheckAccess(AuthorizeOperation operation);
//    Task CheckAccess(AuthorizeOperation operation, params object[] criteria);
//}

///// <summary>
///// Generic Interface so that DI gives us back a different instance per type
///// </summary>
///// <typeparam name="T"></typeparam>
//public interface IAuthorizationRuleManager<T> : IAuthorizationRuleManager
//{
//}

//public class AuthorizationRuleMethod
//{
//    public MethodInfo Method { get; set; }
//    public IAuthorizationRule AuthorizationRule { get; set; }

//}

//public class AuthorizationRuleManager<T> : IAuthorizationRuleManager<T>
//{

//    private IDictionary<AuthorizeOperation, IList<AuthorizationRuleMethod>> AuthorizationMethods = new ConcurrentDictionary<AuthorizeOperation, IList<AuthorizationRuleMethod>>();
//    private bool IsRegistered { get; set; }
//    bool IAuthorizationRuleManager.IsRegistered => IsRegistered;
//    private IServiceProvider Scope { get; set; }

//    public AuthorizationRuleManager(IServiceProvider scope)
//    {
//        Scope = scope; // Needed for AddRule
//        CallRegisterAuthorizationRulesMethod();
//        Scope = null; // No longer needed - Code smell??
//    }

//    /// <summary>
//    /// Call the method with AuthorizationRules attribute on the Domain Object
//    /// </summary>
//    private void CallRegisterAuthorizationRulesMethod()
//    {

//        /// Find the AuthorizationAttribute method; if any
//        var methods = typeof(T).GetMethods(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic)
//            .Where(m => m.GetCustomAttribute<AuthorizationRulesAttribute>() != null).ToList();

//        if (methods.Count > 1)
//        {
//            throw new AuthorzationRulesMethodException($"Only one [{nameof(AuthorizationRulesAttribute)}] allowed per type {typeof(T).FullName}");
//        }

//        if (methods.Count == 1)
//        {
//            var method = methods.Single();

//            if (!method.IsStatic)
//            {
//                throw new AuthorzationRulesMethodException($"AuthorizationRules method is not static on {typeof(T).FullName}");
//            }

//            var parameters = method.GetParameters().ToList();

//            if (parameters.Count != 1)
//            {
//                throw new AuthorzationRulesMethodException($"AuthorizationRules method {typeof(T).FullName} can only have one parameter of type IAuthorizationRuleManager");
//            }

//            if (parameters.Single().ParameterType != typeof(IAuthorizationRuleManager))
//            {
//                throw new AuthorzationRulesMethodException($"AuthorizationRules method {typeof(T).FullName} can only have one parameter of type IAuthorizationRuleManager");
//            }

//            // This is why we keep this Generic
//            // So that we don't pass the ability to change for ALL types
//            method.Invoke(null, new object[] { this });
//        }
//    }

//    /// <summary>
//    /// Authorization rules should have no dependencies or anything on their constructor
//    /// </summary>
//    /// <typeparam name="R"></typeparam>
//    public void AddRule<R>() where R : IAuthorizationRule
//    {

//        R rule = Scope.GetRequiredService<R>();

//        IsRegistered = true;

//        // Deconstruct the methods on the Authorization Rule
//        var methods = rule.GetType().GetMethods(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic)
//            .Where(m => m.GetCustomAttribute<ExecuteAttribute>() != null);

//        foreach (var m in methods)
//        {
//            var operation = m.GetCustomAttribute<ExecuteAttribute>().AuthorizeOperation;

//            if (!(m.ReturnType == typeof(Task<AuthorizationRuleResult>)
//                || m.ReturnType == typeof(AuthorizationRuleResult)))
//            {
//                throw new AuthorizationRuleWrongTypeException($"Execute method must return AuthorizationRuleResult or Task<AuthorizationRuleResult> not {m.ReturnType.FullName}");
//            }

//            if (!AuthorizationMethods.TryGetValue(operation, out var methodInfoList))
//            {
//                AuthorizationMethods.Add(operation, methodInfoList = new List<AuthorizationRuleMethod>());
//            }

//            methodInfoList.Add(new AuthorizationRuleMethod() { Method = m, AuthorizationRule = rule });
//        }

//    }

//    public async Task CheckAccess(AuthorizeOperation operation)
//    {
//        if (IsRegistered)
//        {
//            var methods = AuthorizationRules(operation);
//            var methodFound = false;

//            foreach (var ruleMethod in methods)
//            {
//                var method = ruleMethod.Method;

//                if (!method.GetParameters().Any())
//                {
//                    // Only allow one; maybe take this out later
//                    // AuthorizationRules should be stringent
//                    if (methodFound)
//                    {
//                        throw new AuthorzationRulesMethodException($"More than one {operation.ToString()} method with no criteria found in {ruleMethod.AuthorizationRule.GetType().ToString()}");
//                    }

//                    methodFound = true;
//                    AuthorizationRuleResult ruleResult;
//                    var methodResult = method.Invoke(ruleMethod.AuthorizationRule, new object[0]);
//                    var methodResultAsync = methodResult as Task<AuthorizationRuleResult>;

//                    if (methodResultAsync != null)
//                    {
//                        await methodResultAsync;
//                        ruleResult = ((AuthorizationRuleResult)methodResultAsync.Result);
//                    }
//                    else
//                    {
//                        ruleResult = ((AuthorizationRuleResult)methodResult);
//                    }

//                    if (!ruleResult.HasAccess)
//                    {
//                        throw new AccessDeniedException(ruleResult.Message);
//                    }
//                }
//            }

//            if (!methodFound)
//            {
//                throw new AccessDeniedException($"Missing authorization method for {operation.ToString()} with no criteria");
//            }
//        }
//    }

//    public async Task CheckAccess(AuthorizeOperation operation, params object[] criteria)
//    {
//        if (criteria == null) { throw new ArgumentNullException(nameof(criteria)); }

//        if (IsRegistered)
//        {
//            var methods = AuthorizationRules(operation);
//            var methodFound = false;

//            foreach (var ruleMethod in methods)
//            {
//                var method = ruleMethod.Method;

//                if (method.GetParameters().Count() == criteria.Length)
//                {
//                    var parameterTypes = method.GetParameters().Cast<ParameterInfo>().Select(p => p.ParameterType).GetEnumerator();
//                    var criteriaTypes = criteria.Select(c => c.GetType()).GetEnumerator();
//                    var match = true;

//                    parameterTypes.MoveNext();
//                    criteriaTypes.MoveNext();

//                    while (match && parameterTypes.Current != null && criteriaTypes.Current != null)
//                    {
//                        if (!parameterTypes.Current.IsAssignableFrom(criteriaTypes.Current))
//                        {
//                            match = false;
//                        }

//                        parameterTypes.MoveNext();
//                        criteriaTypes.MoveNext();

//                    }


//                    if (match)
//                    {

//                        // Only allow one; maybe take this out later
//                        // AuthorizationRules should be stringent
//                        if (methodFound)
//                        {
//                            throw new AuthorzationRulesMethodException($"More than one {operation.ToString()} method with no criteria found in {ruleMethod.AuthorizationRule.GetType().ToString()}");
//                        }
//                        methodFound = true;

//                        AuthorizationRuleResult ruleResult;
//                        var methodResult = method.Invoke(ruleMethod.AuthorizationRule, criteria);
//                        var methodResultAsync = methodResult as Task<AuthorizationRuleResult>;

//                        if (methodResultAsync != null)
//                        {
//                            await methodResultAsync;
//                            ruleResult = ((AuthorizationRuleResult)methodResultAsync.Result);
//                        }
//                        else
//                        {
//                            ruleResult = ((AuthorizationRuleResult)methodResult);
//                        }

//                        if (!ruleResult.HasAccess)
//                        {
//                            throw new AccessDeniedException(ruleResult.Message);
//                        }
//                    }
//                }
//            }

//            if (!methodFound)
//            {
//                throw new AccessDeniedException($"Missing authorization method for {operation.ToString()} with criteria [{string.Join(", ", criteria.Select(x => x.GetType().FullName))}]");
//            }

//        }
//    }

//    public IEnumerable<AuthorizationRuleMethod> AuthorizationRules(AuthorizeOperation operation)
//    {
//        if (!AuthorizationMethods.TryGetValue(operation, out var methodInfoList))
//        {
//            AuthorizationMethods.Add(operation, methodInfoList = new List<AuthorizationRuleMethod>());
//        }
//        return methodInfoList;
//    }

//}


//[Serializable]
//public class AuthorizationRuleWrongTypeException : Exception
//{
//    public AuthorizationRuleWrongTypeException() { }
//    public AuthorizationRuleWrongTypeException(string message) : base(message) { }
//    public AuthorizationRuleWrongTypeException(string message, Exception inner) : base(message, inner) { }
//    protected AuthorizationRuleWrongTypeException(
//      System.Runtime.Serialization.SerializationInfo info,
//      System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
//}


//[Serializable]
//public class AuthorzationRulesMethodException : Exception
//{
//    public AuthorzationRulesMethodException() { }
//    public AuthorzationRulesMethodException(string message) : base(message) { }
//    public AuthorzationRulesMethodException(string message, Exception inner) : base(message, inner) { }
//    protected AuthorzationRulesMethodException(
//      System.Runtime.Serialization.SerializationInfo info,
//      System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
//}

//[Serializable]
//public class AccessDeniedException : Exception
//{
//    public AccessDeniedException() { }
//    public AccessDeniedException(string message) : base(message) { }
//    public AccessDeniedException(string message, Exception inner) : base(message, inner) { }
//    protected AccessDeniedException(
//      System.Runtime.Serialization.SerializationInfo info,
//      System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
//}
