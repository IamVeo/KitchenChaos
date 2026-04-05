using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class AuthRequest {
    public string username;
    public string password;
}

[Serializable]
public class JwtResponse {
    public string token;
    public string type;
    public long id;
    public string username;
}


[Serializable]
public class MessageResponse {
    public string message;
}
