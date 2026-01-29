import time
import uuid

class Player:
    def __init__(self, conn, addr, deviceId, nickname):
        self.conn = conn
        self.addr = addr
        self.deviceId = deviceId
        self.nickname = nickname
        self.roomId = None
        self.isReady = False
        self.lastActive = time.time()
        self.sessionToken = str(uuid.uuid4())

    def to_dict_brief(self, hostDeviceId):
        return {
            "nickname": self.nickname,
            "deviceId": self.deviceId,
            "isHost": self.deviceId == hostDeviceId,
            "isReady": self.isReady
        }

class PlayerManager:
    def __init__(self):
        self.players = {}  # deviceId: Player

    def add_player(self, conn, addr, deviceId, nickname):
        if deviceId in self.players:
            return None
        player = Player(conn, addr, deviceId, nickname)
        self.players[deviceId] = player
        return player

    def remove_player(self, deviceId):
        if deviceId in self.players:
            del self.players[deviceId]

    def get_player(self, deviceId):
        return self.players.get(deviceId)

# 전역 인스턴스
player_manager = PlayerManager()