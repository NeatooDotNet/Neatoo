namespace Neatoo.AuthorizationRules;


/// <summary>
/// Place on a static method with parameter IAuthorizationRuleManager(T)
/// to define Authorization Rules for the object
/// </summary>
[System.AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public sealed class AuthorizationRulesAttribute : Attribute
{
    public AuthorizationRulesAttribute()
    {
    }
}

