using Gameframework;
using UnityEngine;

public class ToyManager : SingletonBehaviour<ToyManager>
{
	/*
	 * Manages what toys are locked or unlocked
	 *	- How the heck am I saving that data ?
	 *		- I think this has to be a User Entity, cause I dont want to add more data into Summary Friend Data when
	 *			the data might not be used. Get the user entity when the user visits the player, ensure the loading screen
	 *			waits until the response is completed
	 * Logic for saving picked up currencies
	 *	- not sure to send a request for:
		 * This sounds expensive for # of calls to be billed - each pick up 
		 *	Probably this one -> wait 5 seconds to send bunches of picked up items... 
	 * If the user leaves while having rewards still on the floor, then the manager will pick it up for them
	 * 
	 */
}
