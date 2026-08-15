using System;
using ExtendedXmlSerialization.Cache;

namespace Test_Project_1.ExtendedXMLSerializer
{
    [TestClass]
    public class Test_TypeDefinitionCache
    {
        [TestMethod]
        public void GetDefinition_SameType_ReturnsCachedSameInstance()
        {
            var def1 = TypeDefinitionCache.GetDefinition(typeof(SimplePoco));
            var def2 = TypeDefinitionCache.GetDefinition(typeof(SimplePoco));
            Assert.AreSame(def1, def2);
        }

        [TestMethod]
        public void GetDefinition_DifferentTypes_ReturnDifferentDefinitions()
        {
            var def1 = TypeDefinitionCache.GetDefinition(typeof(SimplePoco));
            var def2 = TypeDefinitionCache.GetDefinition(typeof(NestedPoco));
            Assert.AreNotSame(def1, def2);
        }

        [TestMethod]
        public void GetType_ByAssemblyQualifiedName_ReturnsCorrectType()
        {
            var typeName = typeof(SimplePoco).AssemblyQualifiedName;
            var type = TypeDefinitionCache.GetType(typeName);
            Assert.AreEqual(typeof(SimplePoco), type);
        }

        [TestMethod]
        public void GetType_CalledTwice_UsesCache_ReturnsSameType()
        {
            var typeName = typeof(NestedPoco).AssemblyQualifiedName;
            var type1 = TypeDefinitionCache.GetType(typeName);
            var type2 = TypeDefinitionCache.GetType(typeName);
            Assert.AreEqual(type1, type2);
        }

        [TestMethod]
        public void GetType_ByFullNameOnly_FindsTypeAcrossLoadedAssemblies()
        {
            // Type.GetType(typeName) fails for a bare full name (no assembly qualification),
            // so GetTypeFromName falls back to scanning AppDomain assemblies for a match.
            var type = TypeDefinitionCache.GetType(typeof(SimplePoco).FullName);
            Assert.AreEqual(typeof(SimplePoco), type);
        }

        [TestMethod]
        public void GetTypeFromName_UnknownType_ThrowsException()
        {
            Assert.ThrowsExactly<Exception>(() =>
                TypeDefinitionCache.GetTypeFromName("Totally.Unknown.Type.Name, NoSuchAssembly"));
        }
    }
}
