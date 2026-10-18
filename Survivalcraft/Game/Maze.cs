using System.Collections.Generic;

namespace Game;

public class Maze
{
	private class Room
	{
		private Door topDoor;

		private Door bottonDoor;

		private Door leftDoor;

		private Door rightDoor;

		private Door topLeftDoor;

		private Door topRightDoor;

		private Door bottonLeftDoor;

		private Door bottonRightDoor;

		public Door TopDoor
		{
			get
			{
				return topDoor;
			}
			set
			{
				topDoor = value;
			}
		}

		public Door BottonDoor
		{
			get
			{
				return bottonDoor;
			}
			set
			{
				bottonDoor = value;
			}
		}

		public Door LeftDoor
		{
			get
			{
				return leftDoor;
			}
			set
			{
				leftDoor = value;
			}
		}

		public Door RightDoor
		{
			get
			{
				return rightDoor;
			}
			set
			{
				rightDoor = value;
			}
		}

		public Door TopLeftDoor
		{
			get
			{
				return topLeftDoor;
			}
			set
			{
				topLeftDoor = value;
			}
		}

		public Door TopRightDoor
		{
			get
			{
				return topRightDoor;
			}
			set
			{
				topRightDoor = value;
			}
		}

		public Door BottonLeftDoor
		{
			get
			{
				return bottonLeftDoor;
			}
			set
			{
				bottonLeftDoor = value;
			}
		}

		public Door BottonRightDoor
		{
			get
			{
				return bottonRightDoor;
			}
			set
			{
				bottonRightDoor = value;
			}
		}

		public Room()
		{
			topDoor = new Door();
			bottonDoor = new Door();
			leftDoor = new Door();
			rightDoor = new Door();
			topLeftDoor = new Door();
			topRightDoor = new Door();
			bottonLeftDoor = new Door();
			bottonRightDoor = new Door();
		}
	}

	private class Door
	{
		private bool isLocked;

		private bool isFixed;

		public bool IsFixed
		{
			get
			{
				return isFixed;
			}
			set
			{
				isFixed = value;
			}
		}

		public Door()
		{
			isLocked = true;
		}

		public void OpenTheDoor()
		{
			isLocked = false;
		}

		public void CloseTheDoor()
		{
			isLocked = true;
		}

		public bool GetLockState()
		{
			return isLocked;
		}
	}

	private Room[,] roomMatrix;

	private List<List<Room>> roads;

	private Random random;

	public Maze(int width, int height)
	{
		random = new Random();
		InstRooms(width, height);
		OrganizeRooms();
		SetFixedDoor();
		Interlink();
	}

	private void InstRooms(int width, int height)
	{
		roomMatrix = new Room[width, height];
		roads = new List<List<Room>>();
		for (int i = 0; i < width; i++)
		{
			for (int j = 0; j < height; j++)
			{
				List<Room> list = new List<Room>();
				roomMatrix[i, j] = new Room();
				list.Add(roomMatrix[i, j]);
				roads.Add(list);
			}
		}
	}

	private void OrganizeRooms()
	{
		for (int i = 0; i < roomMatrix.GetLength(0); i++)
		{
			for (int j = 0; j < roomMatrix.GetLength(1) - 1; j++)
			{
				roomMatrix[i, j].BottonDoor = roomMatrix[i, j + 1].TopDoor;
			}
		}
		for (int k = 0; k < roomMatrix.GetLength(1); k++)
		{
			for (int l = 0; l < roomMatrix.GetLength(0) - 1; l++)
			{
				roomMatrix[l, k].RightDoor = roomMatrix[l + 1, k].LeftDoor;
			}
		}
	}

	private void SetFixedDoor()
	{
		for (int i = 0; i < roomMatrix.GetLength(0); i++)
		{
			roomMatrix[i, 0].TopDoor.IsFixed = true;
		}
		for (int j = 0; j < roomMatrix.GetLength(0); j++)
		{
			roomMatrix[j, roomMatrix.GetLength(1) - 1].BottonDoor.IsFixed = true;
		}
		for (int k = 0; k < roomMatrix.GetLength(1); k++)
		{
			roomMatrix[0, k].LeftDoor.IsFixed = true;
		}
		for (int l = 0; l < roomMatrix.GetLength(1); l++)
		{
			roomMatrix[roomMatrix.GetLength(0) - 1, l].RightDoor.IsFixed = true;
		}
	}

	private void Interlink()
	{
		while (!AllRoomLinked())
		{
			List<Door> outlineDoors = GetOutlineDoors(roads[random.Int(0, 1048575) % roads.Count]);
			Door door = outlineDoors[random.Int(0, 1048575) % outlineDoors.Count];
			List<List<Room>> oldRoads = GetOldRoads(door);
			List<Room> newRoad = GetNewRoad(oldRoads);
			RemoveOldRoads(oldRoads);
			roads.Add(newRoad);
			door.OpenTheDoor();
		}
	}

	private void RemoveOldRoads(List<List<Room>> oldRoads)
	{
		foreach (List<Room> oldRoad in oldRoads)
		{
			roads.Remove(oldRoad);
		}
	}

	private List<Room> GetNewRoad(List<List<Room>> oldRoad)
	{
		List<Room> list = new List<Room>();
		foreach (List<Room> item in oldRoad)
		{
			foreach (Room item2 in item)
			{
				list.Add(item2);
			}
		}
		return list;
	}

	private List<List<Room>> GetOldRoads(Door door)
	{
		List<List<Room>> face = new List<List<Room>>();
		foreach (List<Room> road in roads)
		{
			foreach (Room item in road)
			{
				if (TwoDoorAreEqual(item.TopDoor, door))
				{
					face.Add(road);
					break;
				}
				if (TwoDoorAreEqual(item.BottonDoor, door))
				{
					face.Add(road);
					break;
				}
				if (TwoDoorAreEqual(item.LeftDoor, door))
				{
					face.Add(road);
					break;
				}
				if (TwoDoorAreEqual(item.RightDoor, door))
				{
					face.Add(road);
					break;
				}
			}
		}
		return face;
	}

	private bool TwoDoorAreEqual(Door doorSource, Door doorTarget)
	{
		if (doorSource.GetLockState() && !doorSource.IsFixed)
		{
			return object.Equals(doorSource, doorTarget);
		}
		return false;
	}

	private bool AllRoomLinked()
	{
		return roads.Count == 1;
	}

	private List<Door> GetOutlineDoors(List<Room> road)
	{
		List<Door> block2 = new List<Door>();
		foreach (Room item in road)
		{
			AddOutlineDoor(item.TopDoor, block2);
			AddOutlineDoor(item.BottonDoor, block2);
			AddOutlineDoor(item.LeftDoor, block2);
			AddOutlineDoor(item.RightDoor, block2);
		}
		return block2;
	}

	private void AddOutlineDoor(Door door, List<Door> outlineDoors)
	{
		if (door.GetLockState() && !door.IsFixed)
		{
			if (!outlineDoors.Contains(door))
			{
				outlineDoors.Add(door);
			}
			else
			{
				outlineDoors.Remove(door);
			}
		}
	}

	private bool[,] RoomToData()
	{
		bool[,] num2 = new bool[roomMatrix.GetLength(0) * 2 + 1, roomMatrix.GetLength(1) * 2 + 1];
		PreFill(num2);
		for (int i = 0; i < roomMatrix.GetLength(0); i++)
		{
			for (int j = 0; j < roomMatrix.GetLength(1); j++)
			{
				SetData(num2, i, j, -1, 0, roomMatrix[i, j].LeftDoor.GetLockState());
				SetData(num2, i, j, 1, 0, roomMatrix[i, j].RightDoor.GetLockState());
				SetData(num2, i, j, 0, -1, roomMatrix[i, j].TopDoor.GetLockState());
				SetData(num2, i, j, 0, 1, roomMatrix[i, j].BottonDoor.GetLockState());
			}
		}
		return num2;
	}

	private void SetData(bool[,] dataMatrix, int xPos, int yPos, int xOffset, int yOffset, bool isClose)
	{
		dataMatrix[xPos * 2 + 1 + xOffset, yPos * 2 + 1 + yOffset] = isClose;
	}

	private void PreFill(bool[,] dataMatrix)
	{
		for (int i = 0; i < dataMatrix.GetLength(0); i += 2)
		{
			for (int j = 0; j < dataMatrix.GetLength(1); j += 2)
			{
				dataMatrix[i, j] = true;
			}
		}
	}

	public bool[,] GetBoolArray()
	{
		return RoomToData();
	}
}
