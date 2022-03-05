using NUnit.Framework;
using BirdsEverywhere;
using System.Text.Json;
using System.IO;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BirdsEverwhyere.UnitTests
{
    public class BirdDataTests
    {
        private static string projectDir =
    Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\.."));

        private BirdData testBirdData;

        [SetUp]
        public void Setup()
        {
            string in_path = Path.Combine(projectDir, "test_bird.json");
            string jsonFileContent = File.ReadAllText(in_path);
            testBirdData = JsonSerializer.Deserialize<MockBirdData>(jsonFileContent);
        }

        [Test]
        public void BirdData_serialize()
        {
            string out_path = Path.Combine(projectDir, "test_bird_output.json");            
            string jsonOutput = JsonSerializer.Serialize(testBirdData);
            File.WriteAllText(out_path, jsonOutput);
        }

        // defaultSpawnData -> allSpawnData
        // this tests whether SpawnData defaultSpawnData correctly overwrites values in allSpawnData
        [Test]
        public void BirdData_defaultSpawnData_OvewritesEmptyInt()
        {
            Assert.That(testBirdData.allSpawnData["spring"][1].maxGroupCount == 20);
        }
        [Test]
        public void BirdData_defaultSpawnData_OvewritesEmptyListString()
        {
            Assert.That(testBirdData.allSpawnData["spring"][1].locations.SequenceEqual(new List<string>() { "TestBirdLocation_Default_0", "TestBirdLocation_Default_1" }) );
        }
        [Test]
        public void BirdData_defaultSpawnData_DoesntOvewriteValueInt()
        {
            Assert.That(testBirdData.allSpawnData["spring"][1].maxGroupSize == 44);
        }
        [Test]
        public void BirdData_defaultSpawnData_DoesntOvewriteValueListString()
        {
            Assert.That(testBirdData.allSpawnData["spring"][0].locations.SequenceEqual(new List<string>() { "TestBirdLocation_0_Spring_Advanced" }));
        }
        
        // SpawnData default values -> allSpawnData
        // this will test if the default object of SpawnData (SpawnData.getDefaultSpawnData) correctly overwrites values in allSpawnData
        [Test]
        public void BirdData_SpawnDataDefaults_OvewritesEmptyInt()
        {
            Assert.That(testBirdData.allSpawnData["spring"][1].minGroupCount == 1);
        }
        [Test]
        public void BirdData_SpawnDataDefaults_OvewritesEmptyListString()
        {
            Assert.That(testBirdData.allSpawnData["spring"][1].weather.SequenceEqual(new List<string>() { "TestBirdWeather_1_Spring_Advanced_0", "TestBirdWeather_1_Spring_Advanced_1" }));
        }

        [Test]
        public void BirdData_SpawnDataDefaults_DoesntOverwriteValueInt()
        {
            Assert.That(testBirdData.allSpawnData["summer"][0].minGroupSize == 33);
        }

        // spawnPattern and birdType always need to be provided, so they can be null (need to validate in constructor to avoid this)
        [Test]
        public void BirdData_NoDefaultsAndNotGivenRemainsNull()
        {
            Assert.That(testBirdData.allSpawnData["summer"][0].spawnPattern == null);
        }

    }
}