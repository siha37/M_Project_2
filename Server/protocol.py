import json
import time

ERROR_CODES = {
    "JSON_PARSE": 400,
    "ROOM_FULL": 101,
    "NOT_FOUND": 404,
    "FORBIDDEN": 403,
    "UNAUTHORIZED": 401,
    "DUPLICATE": 409,
}

def make_response(success, data=None, error=None):
    return json.dumps({
        "success": success,
        "timestamp": int(time.time()),
        "version": "1.0",
        "data": data,
        "error": error
    }) 