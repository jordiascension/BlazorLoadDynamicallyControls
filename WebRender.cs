using BaseInterfaces;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Mvc;

namespace BlazorReflectionSample
{
    public class WebRender
    {
        private readonly IEnumerable<IControlContract> _controlContract;
        public WebRender(IEnumerable<IControlContract> controlContract)
        {
            _controlContract = controlContract;
        }

        public async Task<RenderFragment> CreateControls()
        {
            return builder =>
            {
                foreach (var controlContract in _controlContract)
                {
                    controlContract.CreateControls(builder);
                }
            };
        }
    }
}
