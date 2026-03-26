using BalancerAPI.Api.Controllers;
using BalancerAPI.Business.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace BalancerAPI.Tests.Controllers;

public class NamesControllerTests
{
    [Fact]
    public async Task Update_ReturnsOkWithUpdatedEntries()
    {
        var expected = new List<UpdatedNameEntry>
        {
            new(Guid.Parse("9f2b2230-3b2c-4b0f-a141-d7b598e236c7"), "oldName", "sumSmash")
        };

        var service = new Mock<INameUpdateService>();
        service.Setup(x => x.UpdateNamesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var controller = new NamesController(service.Object);

        var result = await controller.Update(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<UpdateNamesResponse>(ok.Value);
        var entry = Assert.Single(response.Updated);
        Assert.Equal(expected[0].Uuid, entry.Uuid);
        Assert.Equal("oldName", entry.Previous);
        Assert.Equal("sumSmash", entry.Current);
    }
}