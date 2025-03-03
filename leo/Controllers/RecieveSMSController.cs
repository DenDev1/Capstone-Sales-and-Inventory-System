using leo.Models;

using Microsoft.AspNetCore.Mvc;
using Twilio.AspNet.Core;
using Twilio.TwiML;

namespace leo.Controllers
{

    [ApiController]
    [Route("api/[controller]")]
    public class RecieveSMSController : TwilioController
    {
        [HttpPost("SendReply")]
        public TwiMLResult SendReply([FromForm] TwilioSMS request)
        {
            var response = new MessagingResponse();
            response.Message("Your Product are Out of Stock ");

            return TwiML(response);
        }
    }
}
