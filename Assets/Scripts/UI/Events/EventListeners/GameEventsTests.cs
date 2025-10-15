using System;
using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using PolyDiet.UI.Events;

namespace PolyDiet.Tests
{
    /// <summary>
    /// Testes unitários para o sistema GameEvents
    /// Verifica se os eventos são disparados e recebidos corretamente
    /// </summary>
    public class GameEventsTests
    {
        private int _eventCallCount;
        private string _lastModelName;
        private string _lastVariant;
        private bool _lastCompareMode;
        private Transform _lastCameraTarget;
        
        [SetUp]
        public void SetUp()
        {
            // Limpar estado antes de cada teste
            _eventCallCount = 0;
            _lastModelName = null;
            _lastVariant = null;
            _lastCompareMode = false;
            _lastCameraTarget = null;
            
            // Limpar todos os eventos
            GameEvents.ClearAllEvents();
        }
        
        [TearDown]
        public void TearDown()
        {
            // Limpar eventos após cada teste
            GameEvents.ClearAllEvents();
        }
        
        [Test]
        public void ModelLoaded_Event_ShouldFireCorrectly()
        {
            // Arrange
            string expectedModel = "TestModel";
            string expectedVariant = "original";
            
            GameEvents.OnModelLoaded += (modelName, variant) =>
            {
                _eventCallCount++;
                _lastModelName = modelName;
                _lastVariant = variant;
            };
            
            // Act
            GameEvents.ModelLoaded(expectedModel, expectedVariant);
            
            // Assert
            Assert.AreEqual(1, _eventCallCount, "Event should be called exactly once");
            Assert.AreEqual(expectedModel, _lastModelName, "Model name should match");
            Assert.AreEqual(expectedVariant, _lastVariant, "Variant should match");
        }
        
        [Test]
        public void ModelUnloaded_Event_ShouldFireCorrectly()
        {
            // Arrange
            GameEvents.OnModelUnloaded += () =>
            {
                _eventCallCount++;
            };
            
            // Act
            GameEvents.ModelUnloaded();
            
            // Assert
            Assert.AreEqual(1, _eventCallCount, "Event should be called exactly once");
        }
        
        [Test]
        public void CompareModeChanged_Event_ShouldFireCorrectly()
        {
            // Arrange
            bool expectedMode = true;
            
            GameEvents.OnCompareModeChanged += (isActive) =>
            {
                _eventCallCount++;
                _lastCompareMode = isActive;
            };
            
            // Act
            GameEvents.CompareModeChanged(expectedMode);
            
            // Assert
            Assert.AreEqual(1, _eventCallCount, "Event should be called exactly once");
            Assert.AreEqual(expectedMode, _lastCompareMode, "Compare mode should match");
        }
        
        [Test]
        public void CameraResetRequested_Event_ShouldFireCorrectly()
        {
            // Arrange
            GameEvents.OnCameraResetRequested += () =>
            {
                _eventCallCount++;
            };
            
            // Act
            GameEvents.CameraResetRequested();
            
            // Assert
            Assert.AreEqual(1, _eventCallCount, "Event should be called exactly once");
        }
        
        [Test]
        public void CameraTargetChanged_Event_ShouldFireCorrectly()
        {
            // Arrange
            GameObject testObject = new GameObject("TestTarget");
            Transform expectedTarget = testObject.transform;
            
            GameEvents.OnCameraTargetChanged += (target) =>
            {
                _eventCallCount++;
                _lastCameraTarget = target;
            };
            
            // Act
            GameEvents.CameraTargetChanged(expectedTarget);
            
            // Assert
            Assert.AreEqual(1, _eventCallCount, "Event should be called exactly once");
            Assert.AreEqual(expectedTarget, _lastCameraTarget, "Camera target should match");
            
            // Cleanup
            UnityEngine.Object.DestroyImmediate(testObject);
        }
        
        [Test]
        public void MultipleListeners_ShouldAllReceiveEvents()
        {
            // Arrange
            int listener1Count = 0;
            int listener2Count = 0;
            
            GameEvents.OnModelLoaded += (modelName, variant) => listener1Count++;
            GameEvents.OnModelLoaded += (modelName, variant) => listener2Count++;
            
            // Act
            GameEvents.ModelLoaded("TestModel", "original");
            
            // Assert
            Assert.AreEqual(1, listener1Count, "First listener should receive event");
            Assert.AreEqual(1, listener2Count, "Second listener should receive event");
        }
        
        [Test]
        public void NoListeners_ShouldNotThrowException()
        {
            // Act & Assert - Should not throw exception
            Assert.DoesNotThrow(() =>
            {
                GameEvents.ModelLoaded("TestModel", "original");
                GameEvents.ModelUnloaded();
                GameEvents.CompareModeChanged(true);
                GameEvents.CameraResetRequested();
            });
        }
        
        [Test]
        public void ClearAllEvents_ShouldRemoveAllListeners()
        {
            // Arrange
            GameEvents.OnModelLoaded += (modelName, variant) => _eventCallCount++;
            GameEvents.OnModelUnloaded += () => _eventCallCount++;
            
            // Act
            GameEvents.ClearAllEvents();
            GameEvents.ModelLoaded("TestModel", "original");
            GameEvents.ModelUnloaded();
            
            // Assert
            Assert.AreEqual(0, _eventCallCount, "No events should be called after clearing");
        }
        
        [Test]
        public void GetActiveListenersCount_ShouldReturnCorrectCount()
        {
            // Arrange
            GameEvents.OnModelLoaded += (modelName, variant) => { };
            GameEvents.OnModelLoaded += (modelName, variant) => { };
            GameEvents.OnModelUnloaded += () => { };
            
            // Act
            int count = GameEvents.GetActiveListenersCount();
            
            // Assert
            Assert.AreEqual(3, count, "Should count all active listeners");
        }
        
        [Test]
        public void SystemError_Event_ShouldFireCorrectly()
        {
            // Arrange
            string expectedComponent = "TestComponent";
            string expectedMessage = "Test error message";
            Exception expectedException = new Exception("Test exception");
            
            string receivedComponent = null;
            string receivedMessage = null;
            Exception receivedException = null;
            
            GameEvents.OnSystemError += (component, message, exception) =>
            {
                _eventCallCount++;
                receivedComponent = component;
                receivedMessage = message;
                receivedException = exception;
            };
            
            // Act
            GameEvents.SystemError(expectedComponent, expectedMessage, expectedException);
            
            // Assert
            Assert.AreEqual(1, _eventCallCount, "Event should be called exactly once");
            Assert.AreEqual(expectedComponent, receivedComponent, "Component should match");
            Assert.AreEqual(expectedMessage, receivedMessage, "Message should match");
            Assert.AreEqual(expectedException, receivedException, "Exception should match");
        }
        
        [Test]
        public void LongOperationChanged_Event_ShouldFireCorrectly()
        {
            // Arrange
            string expectedOperation = "TestOperation";
            bool expectedStarted = true;
            
            string receivedOperation = null;
            bool receivedStarted = false;
            
            GameEvents.OnLongOperationChanged += (operation, isStarted) =>
            {
                _eventCallCount++;
                receivedOperation = operation;
                receivedStarted = isStarted;
            };
            
            // Act
            GameEvents.LongOperationChanged(expectedOperation, expectedStarted);
            
            // Assert
            Assert.AreEqual(1, _eventCallCount, "Event should be called exactly once");
            Assert.AreEqual(expectedOperation, receivedOperation, "Operation should match");
            Assert.AreEqual(expectedStarted, receivedStarted, "Started flag should match");
        }
    }
}
