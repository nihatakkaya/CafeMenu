using CafeMenu.Api.Common;

namespace CafeMenu.Tests;

public sealed class ApiResponseTests
{
    [Fact]
    public void SuccessResponse_ShouldCreateSuccessfulResponse()
    {
        var response = ApiResponse<string>.SuccessResponse("data", "Done.");

        Assert.True(response.Success);
        Assert.Equal("Done.", response.Message);
        Assert.Equal("data", response.Data);
    }

    [Fact]
    public void FailureResponse_ShouldCreateFailedResponse()
    {
        var response = ApiResponse<string>.FailureResponse("Failed.");

        Assert.False(response.Success);
        Assert.Equal("Failed.", response.Message);
        Assert.Null(response.Data);
    }
}
