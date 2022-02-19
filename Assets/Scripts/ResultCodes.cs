using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Result
{
    public int ResultCode { get; private set; }
    public string DebugMessage { get; private set; }
    int DisplayTextId;

 

    public Result(int resultCode, string debugMsg)
    {
        ResultCode = resultCode;
        DebugMessage = debugMsg;

        // Resolve display text...
    }

    public Result(int resultCode)
    {
        ResultCode = resultCode;
    }

    public static Result GetSuccess() { return new Result(RC_SUCCESS); }
    public static Result GetFailure() { return new Result(RC_FAILURE); }

    public bool IsSuccess() { return ResultCode == RC_SUCCESS; }
    
    public const int RC_FAILURE = -1;
    public const int RC_SUCCESS = 0;
    // Add here...
}