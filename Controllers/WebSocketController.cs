using System.Collections.Concurrent;
using System.Net;
using System.Net.WebSockets;
using Microsoft.AspNetCore.Mvc;
using models;

namespace WebSocketAPI.Controllers;

[ApiController]
[Route("[controller]")]
public class WebSocketController  : ControllerBase
{   

    protected static ConcurrentDictionary<User, string> OnlineUsers = new ConcurrentDictionary<User, string>();


    [HttpGet]
    public async Task Get()
    {
        var context = HttpContext;

        if (!context.WebSockets.IsWebSocketRequest)
            context.Response.StatusCode = (int) HttpStatusCode.BadRequest;
        else
        {
            try{
                
                using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
                OnlineUsers.TryAdd(new User(){Context=webSocket}, String.Empty);
               

                var readbuf = new ArraySegment<byte>(new Byte[65535]);

                while(true){
                    var msg = await webSocket.ReceiveAsync(readbuf, CancellationToken.None);

                    if(msg.MessageType == WebSocketMessageType.Close){

                        var user = OnlineUsers.Keys.Where(u => u.Context == webSocket).Single();
                        OnlineUsers.TryRemove(user, out _);

                        await webSocket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, 
                                                        "Connection close by client",
                                                        CancellationToken.None);
                        return;
                    }else{
                    
                        var payload =  new ArraySegment<byte>(readbuf.Array, readbuf.Offset, msg.Count);  

                        foreach( var user in OnlineUsers.Keys){
                            if(user.Context.State != WebSocketState.Closed){
                                await user.Context.SendAsync(payload, msg.MessageType, true, CancellationToken.None);
                            }
                           
                        }          
                    }
                }
            }catch(Exception e ){
                Console.WriteLine($"Error {e}");
            }
        
        }
    }

    [HttpGet]
    [Route("clear")]
    public async Task ClearList(){
        OnlineUsers.Clear();
    }
}
