import time

class RoomInfo:
    def __init__(self, roomId, hostDeviceId, hostAddress, hostPort, maxPlayers, roomName, gameType, isPrivate, joinCode=None):
        self.roomId = roomId
        self.hostDeviceId = hostDeviceId
        self.hostAddress = hostAddress
        self.hostPort = hostPort
        self.maxPlayers = maxPlayers
        self.currentPlayers = 0
        self.status = "active"  # 방 생성 시 바로 active 상태로 설정
        self.roomName = roomName
        self.gameType = gameType
        self.createdTime = int(time.time())
        self.lastHeartbeat = int(time.time())
        self.lastActivity = int(time.time())
        self.isPrivate = isPrivate
        self.joinCode = joinCode

    def to_dict(self):
        return self.__dict__

class RoomManager:
    def __init__(self):
        self.rooms = {}  # roomId: RoomInfo
        self.room_players = {}  # roomId: set(deviceId)

    def create_room(self, data):
        required_fields = ["roomId", "deviceId", "hostAddress", "hostPort", "maxPlayers", "roomName"]
        for field in required_fields:
            if field not in data or data[field] is None:
                print(f"[방 생성 오류] 필수 필드 누락: {field}, data={data}")
                return None, f"필수 필드 누락: {field}"
        roomId = data["roomId"]
        if roomId in self.rooms:
            return None, "이미 존재하는 방 ID"
        room = RoomInfo(
            roomId=roomId,
            hostDeviceId=data["deviceId"],
            hostAddress=data["hostAddress"],
            hostPort=data["hostPort"],
            maxPlayers=data["maxPlayers"],
            roomName=data["roomName"],
            gameType=data.get("gameType", "mafia"),
            isPrivate=data.get("isPrivate", False),
            joinCode=data.get("joinCode")
        )
        self.rooms[roomId] = room
        self.room_players[roomId] = set()
        return room, None

    def join_room(self, roomId, deviceId):
        if roomId not in self.rooms:
            return None, "방이 존재하지 않습니다"
        room = self.rooms[roomId]
        if len(self.room_players[roomId]) >= room.maxPlayers:
            return None, "방이 가득 찼습니다"
        self.room_players[roomId].add(deviceId)
        room.currentPlayers = len(self.room_players[roomId])
        room.lastActivity = int(time.time())
        # 인원이 최대치에 도달하면 status를 'full'로 변경
        if room.currentPlayers >= room.maxPlayers:
            room.status = "full"
        else:
            room.status = "active"
        return room, None

    def leave_room(self, roomId, deviceId):
        if roomId not in self.rooms:
            return False, "방이 존재하지 않습니다"
        if deviceId in self.room_players[roomId]:
            self.room_players[roomId].remove(deviceId)
            self.rooms[roomId].currentPlayers = len(self.room_players[roomId])
            self.rooms[roomId].lastActivity = int(time.time())
            # 방장 위임
            if deviceId == self.rooms[roomId].hostDeviceId:
                if self.room_players[roomId]:
                    self.rooms[roomId].hostDeviceId = next(iter(self.room_players[roomId]))
                else:
                    from server_status import log_server_event
                    log_server_event(f"[INFO] 방 삭제(호스트 퇴장): roomId={roomId}")
                    del self.rooms[roomId]
                    del self.room_players[roomId]
                    return True, None
            # 방에 아무도 없으면 삭제
            if not self.room_players[roomId]:
                from server_status import log_server_event
                log_server_event(f"[INFO] 방 삭제(빈 방): roomId={roomId}")
                del self.rooms[roomId]
                del self.room_players[roomId]
                return True, None
            # 인원이 최대치 미만이 되면 status를 'active'로 변경
            if self.rooms[roomId].currentPlayers < self.rooms[roomId].maxPlayers:
                self.rooms[roomId].status = "active"
            return True, None
        return False, "플레이어가 방에 없습니다"

    def get_room_list(self, include_private=False):
        return [
            room.to_dict()
            for room in self.rooms.values()
            if include_private or not room.isPrivate
        ]

    def get_player_list(self, roomId, player_manager):
        return [
            player_manager.get_player(deviceId).to_dict_brief(self.rooms[roomId].hostDeviceId)
            for deviceId in self.room_players.get(roomId, [])
        ]

# main.py 등에서 import할 수 있도록 RoomManager 인스턴스 생성
room_manager = RoomManager() 