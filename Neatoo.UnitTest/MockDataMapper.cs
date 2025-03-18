//using Moq;
//using Neatoo.Internal;
//using Neatoo.RemoteFactory;
//using System.Threading.Tasks;

//namespace Neatoo.UnitTest;

//public class MockDataMapper<T> : INeatooPortal<T>
//{
//    public MockDataMapper()
//    {
//        MockPortal = new Mock<INeatooPortal<T>>(MockBehavior.Strict);
//    }

//    public Mock<INeatooPortal<T>> MockPortal { get; }

//    public Task<T> Create()
//    {
//        return MockPortal.Object.Create();
//    }
//    public Task<T> Create(object[] criteria)
//    {
//        return MockPortal.Object.Create(criteria);
//    }
//    public Task<T> Fetch()
//    {
//        return MockPortal.Object.Fetch();
//    }
//    public Task<T> Fetch(object[] criteria)
//    {
//        return MockPortal.Object.Fetch(criteria);
//    }
//    public Task<T> Update<T1>(T1 target) where T1 : T, IEditMetaProperties
//    {
//        return MockPortal.Object.Update(target);
//    }
//    public Task<T> Update<T1>(T1 target, params object[] criteria) where T1 : T, IEditMetaProperties
//    {
//        return MockPortal.Object.Update(target, criteria);
//    }
//    public Task<T> CreateChild()
//    {
//        return MockPortal.Object.CreateChild();
//    }
//    public Task<T> FetchChild()
//    {
//        return MockPortal.Object.FetchChild();
//    }
//    public Task<T> CreateChild(object[] criteria)
//    {
//        return MockPortal.Object.CreateChild(criteria);
//    }
//    public Task<T> FetchChild(object[] criteria)
//    {
//        return MockPortal.Object.FetchChild(criteria);
//    }

//    public Task<T> Execute<T1>() where T1 : IRemoteMethodPortal<T>, T
//    {
//        throw new System.NotImplementedException();
//    }

//    public Task<T> Execute<T1, P>(P parameters)
//        where T1 : IRemoteMethodPortal<T, P>, T
//        where P : notnull
//    {
//        throw new System.NotImplementedException();
//    }
//}
