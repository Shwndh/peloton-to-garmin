﻿using Api.Controllers;
using Common.Dto.Api;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Moq.AutoMock;
using NUnit.Framework;
using Sync;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ErrorResponse = Common.Dto.Api.ErrorResponse;
using SyncErrorResponse = Sync.ErrorResponse;

namespace UnitTests.Api.Controllers
{
	public class SyncControllerTests
	{
		[Test]
		public async Task SyncAsync_With_NullRequest_Returns400()
		{
			var autoMocker = new AutoMocker();
			var controller = autoMocker.CreateInstance<SyncController>();

			var response = await controller.SyncAsync(null);

			var result = response.Result as BadRequestObjectResult;
			result.Should().NotBeNull();
			var value = result.Value as ErrorResponse;
			value.Message.Should().Be("Either NumWorkouts or WorkoutIds must be set.");
		}

		[Test]
		public async Task SyncAsync_With_DefaultRequest_Returns400()
		{
			var autoMocker = new AutoMocker();
			var controller = autoMocker.CreateInstance<SyncController>();

			var request = new SyncPostRequest();

			var response = await controller.SyncAsync(request);

			var result = response.Result as BadRequestObjectResult;
			result.Should().NotBeNull();
			var value = result.Value as ErrorResponse;
			value.Message.Should().Be("Either NumWorkouts or WorkoutIds must be set.");
		}

		[Test]
		public async Task SyncAsync_With_NegativeNumWorkoutsRequest_Returns400()
		{
			var autoMocker = new AutoMocker();
			var controller = autoMocker.CreateInstance<SyncController>();

			var request = new SyncPostRequest() { NumWorkouts = -1 };

			var response = await controller.SyncAsync(request);

			var result = response.Result as BadRequestObjectResult;
			result.Should().NotBeNull();
			var value = result.Value as ErrorResponse;
			value.Message.Should().Be("Either NumWorkouts or WorkoutIds must be set.");
		}

		[Test]
		public async Task SyncAsync_With_EmptyWorkoutIdsRequest_Returns400()
		{
			var autoMocker = new AutoMocker();
			var controller = autoMocker.CreateInstance<SyncController>();

			var request = new SyncPostRequest() { NumWorkouts = 0, WorkoutIds = new List<string>() };

			var response = await controller.SyncAsync(request);

			var result = response.Result as BadRequestObjectResult;
			result.Should().NotBeNull();
			var value = result.Value as ErrorResponse;
			value.Message.Should().Be("Either NumWorkouts or WorkoutIds must be set.");
		}

		[Test]
		public async Task SyncAsync_With_BothParamsSet_Returns400()
		{
			var autoMocker = new AutoMocker();
			var controller = autoMocker.CreateInstance<SyncController>();

			var request = new SyncPostRequest() { NumWorkouts = 1, WorkoutIds = new List<string>() { "someId" } };

			var response = await controller.SyncAsync(request);

			var result = response.Result as BadRequestObjectResult;
			result.Should().NotBeNull();
			var value = result.Value as ErrorResponse;
			value.Message.Should().Be("NumWorkouts and WorkoutIds cannot both be set.");
		}

		[Test]
		public async Task SyncAsync_NumWorkouts_Calls_CorrectMethod()
		{
			var autoMocker = new AutoMocker();
			var controller = autoMocker.CreateInstance<SyncController>();
			var service = autoMocker.GetMock<ISyncService>();
			service.SetReturnsDefault(Task.FromResult(new SyncResult() { SyncSuccess = true }));

			var request = new SyncPostRequest() { NumWorkouts = 5 };

			var actionResult = await controller.SyncAsync(request);

			var response = actionResult.Result as CreatedResult;
			response.Should().NotBeNull();

			service.Verify(s => s.SyncAsync(It.IsAny<int>()), Times.Once);
		}

		[Test]
		public async Task SyncAsync_WorkoutIds_Calls_CorrectMethod()
		{
			var autoMocker = new AutoMocker();
			var controller = autoMocker.CreateInstance<SyncController>();
			var service = autoMocker.GetMock<ISyncService>();
			service.SetReturnsDefault(Task.FromResult(new SyncResult() { SyncSuccess = true }));

			var request = new SyncPostRequest() { NumWorkouts = 0, WorkoutIds = new List<string>() { "someId" } };

			var actionResult = await controller.SyncAsync(request);

			var response = actionResult.Result as CreatedResult;
			response.Should().NotBeNull();

			service.Verify(s => s.SyncAsync(It.IsAny<ICollection<string>>(), null), Times.Once);
		}

		[Test]
		public async Task SyncAsync_When_Service_Throws_Exception_Returns_BadRequest()
		{
			var autoMocker = new AutoMocker();
			var controller = autoMocker.CreateInstance<SyncController>();
			var service = autoMocker.GetMock<ISyncService>();

			service.Setup(s => s.SyncAsync(It.IsAny<int>()))
				.Throws(new Exception("Some unhandled case."));

			var request = new SyncPostRequest() { NumWorkouts = 5 };

			var actionResult = await controller.SyncAsync(request);

			var result = actionResult.Result as ObjectResult;
			result.Should().NotBeNull();
			result.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
			var value = result.Value as ErrorResponse;
			value.Message.Should().Be("Unexpected error occurred: Some unhandled case.");
		}

		[TestCase(5, new string[0])]
		[TestCase(0, new string[1] { "someId" })]
		public async Task SyncAsync_When_SyncUnsuccessful_OkResult_Returned(int numWorkouts, string[] workoutIds)
		{
			var autoMocker = new AutoMocker();
			var controller = autoMocker.CreateInstance<SyncController>();
			var service = autoMocker.GetMock<ISyncService>();
			service.SetReturnsDefault(Task.FromResult(new SyncResult() { SyncSuccess = false }));

			var request = new SyncPostRequest() { NumWorkouts = numWorkouts, WorkoutIds = workoutIds };

			var actionResult = await controller.SyncAsync(request);

			var response = actionResult.Result as OkObjectResult;
			response.Should().NotBeNull();

			var result = response.Value as SyncPostResponse;
			result.SyncSuccess.Should().BeFalse();
		}

		[TestCase(5, new string[0])]
		[TestCase(0, new string[1] { "someId" })]
		public async Task SyncAsync_When_SyncSuccessful_CreatedResult_Returned(int numWorkouts, string[] workoutIds)
		{
			var autoMocker = new AutoMocker();
			var controller = autoMocker.CreateInstance<SyncController>();
			var service = autoMocker.GetMock<ISyncService>();
			service.SetReturnsDefault(Task.FromResult(new SyncResult()
			{
				SyncSuccess = true,
				PelotonDownloadSuccess = true,
				ConversionSuccess = true,
				UploadToGarminSuccess = true
			}));

			var request = new SyncPostRequest() { NumWorkouts = numWorkouts, WorkoutIds = workoutIds };

			var actionResult = await controller.SyncAsync(request);

			var response = actionResult.Result as CreatedResult;
			response.Should().NotBeNull();

			var result = response.Value as SyncPostResponse;
			result.SyncSuccess.Should().BeTrue();
			result.PelotonDownloadSuccess.Should().BeTrue();
			result.ConverToFitSuccess.Should().BeTrue();
			result.UploadToGarminSuccess.Should().BeTrue();
			result.Errors.Should().BeNullOrEmpty();
		}

		[TestCase(5, new string[0] )]
		[TestCase(0, new string[1] {"someId"})]
		public async Task SyncAsync_When_SyncErrors_MapsErrorsCorrectly(int numWorkouts, string[] workoutIds)
		{
			var autoMocker = new AutoMocker();
			var controller = autoMocker.CreateInstance<SyncController>();
			var service = autoMocker.GetMock<ISyncService>();

			var syncResult = new SyncResult()
			{
				SyncSuccess = false,
			};
			syncResult.Errors.Add(new SyncErrorResponse() { Message = "error 1" });
			syncResult.Errors.Add(new SyncErrorResponse() { Message = "error 2" });
			syncResult.Errors.Add(new SyncErrorResponse() { Message = "error 3" });
			service.SetReturnsDefault(Task.FromResult(syncResult));

			var request = new SyncPostRequest() { NumWorkouts = numWorkouts, WorkoutIds = workoutIds };

			var actionResult = await controller.SyncAsync(request);

			var response = actionResult.Result as OkObjectResult;
			response.Should().NotBeNull();

			var result = response.Value as SyncPostResponse;
			result.SyncSuccess.Should().BeFalse();
			result.Errors.Should().NotBeNullOrEmpty();
			result.Errors.Count.Should().Be(3);
			result.Errors.ElementAt(0).Message.Should().Be("error 1");
			result.Errors.ElementAt(1).Message.Should().Be("error 2");
			result.Errors.ElementAt(2).Message.Should().Be("error 3");
		}
	}
}
