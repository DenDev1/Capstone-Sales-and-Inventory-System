using Microsoft.AspNetCore.Mvc;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.TwiML.Messaging;
using leo.Models;

namespace leo.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SendSMSController : ControllerBase
    {
        string accountSid = "AC5cdbdb1e837ad374e5243da84f2f05c0";
        string authToken = "1bfe916372cd90b54f26fc5100944813";

        [HttpPost("SendText")]
        public ActionResult SendText(string phonenumber)
        {
            TwilioClient.Init(accountSid, authToken);

            var message = MessageResource.Create(

                body: "Hi Your Item are Out Of Stock ",
                from: new Twilio.Types.PhoneNumber("+12532043283"),
                to: new Twilio.Types.PhoneNumber("+63" + phonenumber)

                );

            return StatusCode(200, new { message = message.Sid });
        }

    }
}
