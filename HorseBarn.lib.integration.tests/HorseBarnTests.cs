using Microsoft.Extensions.DependencyInjection;
using HorseBarn.Dal.Ef;
using HorseBarn.lib.Horse;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Moq;
using System.Data;
using Neatoo;

namespace HorseBarn.lib.integration.tests;

[TestClass]
public sealed class HorseBarnTests
{
    private IServiceScope scope;
    private IHorseBarnFactory portal;
    private HorseBarnContext horseBarnContext;
    private HorseCriteriaFactory horseCriteriaFactory;
    private IDbContextTransaction transaction;

    [TestInitialize]
    public async Task TestInitialize()
    {

        scope = UnitTestContainer.GetLifetimeScope();
        portal = scope.ServiceProvider.GetRequiredService<IHorseBarnFactory>();
        horseBarnContext = scope.ServiceProvider.GetRequiredService<HorseBarnContext>();
        horseCriteriaFactory = scope.ServiceProvider.GetRequiredService<HorseCriteriaFactory>();

        await horseBarnContext.Horses.ExecuteDeleteAsync();
        await horseBarnContext.Carts.ExecuteDeleteAsync();
        await horseBarnContext.Pastures.ExecuteDeleteAsync();
        await horseBarnContext.HorseBarns.ExecuteDeleteAsync();

        transaction = await horseBarnContext.Database.BeginTransactionAsync();
    }

    [TestCleanup]
    public void TestCleanup()
    {
        transaction.Rollback();
        scope.Dispose();
    }

    [TestMethod]
    public async Task HorseBarn_FullRun()
    {
        var horseBarn = portal.Create();

        Assert.IsTrue(horseBarn.IsValid);
        Assert.IsTrue(horseBarn.IsNew);
        Assert.IsTrue(horseBarn.IsModified);

        async Task AddCartToHorseBarn()
        {
            var criteria = horseCriteriaFactory.Fetch();

            criteria.Name = "Heavy Horse A";
            criteria.Breed = Breed.Clydesdale;
            criteria.BirthDay = DateOnly.FromDateTime(DateTime.Now);

            var heavyHorse = (IHeavyHorse)horseBarn.AddNewHorse(criteria);

            Assert.IsTrue(horseBarn.IsValid);

            Assert.IsInstanceOfType<IHeavyHorse>(heavyHorse);

            criteria.Name = "Light Horse B";
            criteria.Breed = Breed.Thoroughbred;

            var lightHorse = (ILightHorse)horseBarn.AddNewHorse(criteria);

            Assert.IsInstanceOfType<ILightHorse>(lightHorse);

            Assert.IsTrue(horseBarn.IsValid);

            var racingChariot = await horseBarn.AddRacingChariot();

            var wagon = await horseBarn.AddWagon();

            Assert.IsFalse(horseBarn.IsValid);

            racingChariot.Name = "Racing Chariot";
            wagon.Name = "Wagon";

            Assert.IsTrue(horseBarn.IsValid);

            wagon.NumberOfHorses = 2;

            // Key: Cannot add an ILightHorse to the wagon 
            // No validation, no if statements
            horseBarn.MoveHorseToCart(heavyHorse, wagon);

            Assert.IsFalse(horseBarn.IsValid);

            criteria.Name = "Heavy Horse B";
            criteria.Breed = Breed.Clydesdale;

            heavyHorse = (IHeavyHorse)horseBarn.AddNewHorse(criteria);

            horseBarn.MoveHorseToCart(heavyHorse, wagon);

            Assert.IsTrue(horseBarn.IsValid);
        }

        await AddCartToHorseBarn();

        horseBarn = (IHorseBarn)await horseBarn.Save();

        var horseBarnContext = scope.ServiceProvider.GetRequiredService<IHorseBarnContext>();

        var horses = await horseBarnContext.Horses.ToListAsync();

        Assert.AreEqual(3, horses.Count);
        CollectionAssert.AreEquivalent(horseBarn.Horses.Select(h => h.Id).ToList(), horses.Select(h => h.Id).ToList());

        var carts = await horseBarnContext.Carts.ToListAsync();
        CollectionAssert.AreEquivalent(horseBarn.Carts.Select(c => c.Id).ToList(), carts.Select(c => c.Id).ToList());

        var pasture = await horseBarnContext.Pastures.ToListAsync();
        Assert.AreEqual(pasture.Single().Id, horseBarn.Pasture.Id);

        horseBarn = await portal.Fetch();

        await AddCartToHorseBarn();

        foreach (var item in horseBarn.Horses)
        {
            item.Name = Guid.NewGuid().ToString();
        }

        var horseNames = horseBarn.Horses.Select(h => h.Name).ToList();

        // Mix of Inserts and Updates
        horseBarn = (IHorseBarn) await horseBarn.Save();

        Assert.IsFalse(horseBarn.IsModified); // TODO
        CollectionAssert.AreEquivalent(horseNames, horseBarnContext.Horses.Select(h => h.Name).ToList());
        CollectionAssert.AreEquivalent(horseBarn.Carts.Select(c => c.Id).ToList(), horseBarnContext.Carts.Select(c => c.Id).ToList());
        Assert.AreEqual(horseBarn.Pasture.Id, horseBarnContext.Pastures.Single().Id);


        horseBarn = await portal.Fetch();

        Assert.IsFalse(horseBarn.IsModified);
    }

}
