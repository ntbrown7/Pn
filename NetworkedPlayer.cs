using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;

public class NetworkedPlayer : NetworkBehaviour
{
    [SyncVar]
    public int health;
    [SyncVar]
    public int points;
    [SyncVar]
    public string playerName;
    [SyncVar]
    public string playerCharacter;
    [SyncVar]
    public GameObject playerObject;
    /* This can be used for a pregame countdown, which will be useful for slow connections and such */
    [SyncVar]
    public bool canMove = true;
    [SyncVar]
    public bool canPlay = true; //Determined by Lobby
    
    [SyncVar]
    public bool respawning = true;
    public Sprite hearts;
    [SyncVar]
    public int characterIndex = 1;
    public GameObject selectedPlayerPrefab;
    public BoxCollider2D[] playerColliders;
    public GameObject[] prefabCharacters;
    private GameObject health1, health2, health3;
    public SpriteRenderer[] characterSelector;
    public Animator[] characterAnim;
    public Sprite[] Sprites;
    public Text winText;
    public Image winImage;
    public GameObject winCanvas;
    
    [SyncVar]
    public bool isJumping;
    public bool facingLeft = false;
    [SyncVar]
    public float axisH;
    [SyncVar]
    public Vector3 mousePosition;
    private NetworkStartPosition[] spawnPoints;

    [SyncVar]
    public bool canShoot = true;
    [SyncVar]
    public bool canPlunge = true;
    int animationChanger = 0;

    public bool canJump = true;
    [SyncVar]
    int numPlayers;
    public float shootTimer;
    public float plungeTimer;
    private float playX;
    private float playY;
    public float mouseX { get; private set; }

    private float mouseY;
    public float projectileSpeed = 15;
    public GameObject glove;
    public GameObject plunger;
    private Text playerScoreText;
    private Text playerNameText;
    Text Timer;
    MapTimers currentMap;
    int timeInGame;

	public void timerSet(int gameTime){
		timeInGame = gameTime;
	}
    [ClientRpc]
    public void Rpc_timerSet(int gameTime){
        if(isClient){
            timeInGame = gameTime;
        }
    }

    //The next two functions will start the cooldown for shooting and plunging respectively
    public IEnumerator StartPunchCountdown(float countdownValue)
    {
        this.canShoot = false;
        this.shootTimer = countdownValue;
        while (this.shootTimer > 0)
        {
            yield return new WaitForSeconds(1.0f);
            this.shootTimer--;
        }
        this.canShoot = true;
        //Debug.Log("Punch Ready");
    }
    public IEnumerator StartPlungeCountdown(float countdownValue)
    {
        this.canPlunge = false;
        this.plungeTimer = countdownValue;
        while (this.plungeTimer > 0)
        {
            yield return new WaitForSeconds(1.0f);
            this.plungeTimer--;
        }
        this.canPlunge = true;
        //Debug.Log("Plunge Ready");
    }
    [Command]
    public void Cmd_stopSpawning(){
        respawning = false;
    }
    public IEnumerator respawnTimer(int respawnTimer){
        Debug.Log("Trying to respawn a player");
        canMove = false;
        //This is called so everyone can see the timer(hopefully)
        Rpc_updateRespawnTimerText(GameObject.Find(this.gameObject.name + "Timer"), respawnTimer);
        
        while(respawnTimer > 0){
            yield return new WaitForSeconds(1f);
            respawnTimer--;
            Rpc_updateRespawnTimerText(GameObject.Find(this.gameObject.name + "Timer"), respawnTimer);
            updateRespawnTimerText(GameObject.Find(this.gameObject.name + "Timer"), respawnTimer);
        }
        Cmd_playerDrop();
    }
    [ClientRpc]
    void Rpc_updateRespawnTimerText(GameObject timerTextObject, int time){
        if(isClient){
            timerTextObject.GetComponent<Text>().text = time.ToString();
        }
    }

    void updateRespawnTimerText(GameObject timerTextObject, int time)
    {
        timerTextObject.GetComponent<Text>().text = time.ToString();
    }
    [Command]
    public void Cmd_playerDrop(){
        //player hearts scale is 93.165;
        NetworkedPlayerMovement playerMovement = this.playerObject.GetComponent<NetworkedPlayerMovement>();
        int spawningLocation = Random.Range(0,3);
        playerObject.transform.position = spawnPoints[spawningLocation].transform.position;

        resetHearts();
        Rpc_resetHearts();

        this.health = 3;
        playerMovement.canMove = true;
    }
    private void resetHearts(){
        health1.GetComponent<SpriteRenderer>().transform.localScale = new Vector3(93.165f, 93.165f, 93.165f);
        health2.GetComponent<SpriteRenderer>().transform.localScale = new Vector3(93.165f, 93.165f, 93.165f);
        health3.GetComponent<SpriteRenderer>().transform.localScale = new Vector3(93.165f, 93.165f, 93.165f);
    }
    [ClientRpc]
    private void Rpc_resetHearts(){
        if(isClient){
            health1.GetComponent<SpriteRenderer>().transform.localScale = new Vector3(93.165f, 93.165f, 93.165f);
            health2.GetComponent<SpriteRenderer>().transform.localScale = new Vector3(93.165f, 93.165f, 93.165f);
            health3.GetComponent<SpriteRenderer>().transform.localScale = new Vector3(93.165f, 93.165f, 93.165f);
        }
    }
    [Command]
    public void Cmd_restartPlayer(){
        NetworkedPlayerMovement playerMovement = this.playerObject.GetComponent<NetworkedPlayerMovement>();
        playerObject.transform.position = GameObject.Find("DeathBox").transform.position;
        playerMovement.canMove = false;
        StartCoroutine(respawnTimer(5));
    }
    /* Damage taking code is below */
    [Command]
    public void Cmd_TakeDamage(int amount)
    {
        health -= amount;
        Debug.Log(this.gameObject.name + " health: " + health);
        if (health <= 0)
        {
            Debug.Log("Putting player in THE BOX");
            updateHearts();
            Rpc_updateHearts();
            Cmd_restartPlayer();
        }
        updateHearts();
        Rpc_updateHearts();
    }
    /* Updating the hearts that are displayed */
    public void updateHearts(){
        switch(health){
            case(0):
                health1.GetComponent<SpriteRenderer>().transform.localScale = new Vector3(0, 0, 0);
                Debug.Log(health);
            break;
            case(1):
                health2.GetComponent<SpriteRenderer>().transform.localScale = new Vector3(0, 0, 0);
                Debug.Log(health);
                break;
            case(2):
                health3.GetComponent<SpriteRenderer>().transform.localScale = new Vector3(0, 0, 0);
                Debug.Log(health);
            break;
            default:
                Debug.Log(health + " accessing with health");
            
            break;
        } 
    }
    public void Rpc_updateHearts(){
        if(isClient){
            switch (health)
            {
                case (0):
                    health1.GetComponent<SpriteRenderer>().transform.localScale = new Vector3(0, 0, 0);
                    Debug.Log(health);
                    break;
                case (1):
                    health2.GetComponent<SpriteRenderer>().transform.localScale = new Vector3(0, 0, 0);
                    Debug.Log(health);
                    break;
                case (2):
                    health3.GetComponent<SpriteRenderer>().transform.localScale = new Vector3(0, 0, 0);
                    Debug.Log(health);
                    break;
                default:
                    Debug.Log(health + " accessing with health");

                    break;
            }  
        }
    }
    
    //This is Sending to all RPCs to Declare a winner, the UI should show the character, name, points, and a back button
    [ClientRpc]
    public void Rpc_DeclareWinner(string playerName, int points, int index)
    {
        //Debug.Log(playerName + " has won the game with "+ points +" point(s)" );
        if(isClient){
            winText.text = playerName + " has won the game with " + points + " point(s)";
            winImage.sprite = Sprites[index];
            winCanvas.SetActive(true);
        }
    }
    [Command]
    public void Cmd_SendMovements(float axis, bool jumping, Vector3 mousePosition)
    {
        this.axisH = axis;
        this.isJumping = jumping;
        this.mousePosition = mousePosition;
    }
    public void StartRappelC(Vector3 point, float seconds)
    {
        StartCoroutine(StartRappel(point, seconds));
    }
    public IEnumerator StartRappel(Vector3 point, float seconds)
    {
        float timer = 0.0f;
        NetworkedPlayerMovement playerMovement = this.playerObject.GetComponent<NetworkedPlayerMovement>();
        Rigidbody2D rb = this.playerObject.GetComponent<Rigidbody2D>();
        Vector2 direction = ((Vector2)(point - this.playerObject.transform.position)).normalized;
        //Debug.Log(direction); 
        //Debug.Log(this.playerObject.transform.position);
        playerMovement.canMove = false;
        while (timer < seconds && !playerMovement.canMove && !isJumping && Vector2.Distance(point, this.playerObject.transform.position) > 1)
        {
            //Debug.Log(timer);
            //rb.MovePosition(transform.position + direction * 10 * Time.deltaTime);
            rb.velocity = direction * 10;
            yield return new WaitForSeconds(0.05f);
            timer += 0.05f;
        }
        playerMovement.canMove = true;
    }

    public void StartPlungeC(Vector3 angle, float seconds)
    {
        canShoot = false;
        StartCoroutine(StartPlunge(angle, seconds));
    }

    public IEnumerator StartPlunge(Vector3 angle, float seconds)
    {
        float timer = 0.0f;
        NetworkedPlayerMovement playerMovement = this.playerObject.GetComponent<NetworkedPlayerMovement>();
        Rigidbody2D rb = this.playerObject.GetComponent<Rigidbody2D>();

        //Debug.Log(direction);
        playerMovement.canMove = false;
        while (timer < seconds && !playerMovement.canMove)
        {
          if(timer < 0.20f && Input.GetAxis("jump") > 0){
            timer = seconds;
            playerMovement.canMove = true;
            Cmd_SendMovements(Input.GetAxis("horizontal"),  Input.GetAxis("jump") > 0, Camera.main.ScreenToWorldPoint(Input.mousePosition));
          }
            //Debug.Log(timer);
            rb.velocity = angle * 10;
            yield return new WaitForSeconds(0.05f);
            timer += 0.05f;
        }
        canShoot = true;
        playerMovement.canMove = true;
    }
    [Command]
    public void Cmd_SendShot(Vector3 axis, string type)
    {

        float AngleRad = Mathf.Atan2(this.mousePosition.y - this.playerObject.transform.position.y, this.mousePosition.x - this.playerObject.transform.position.x);
        float angle = (180 / Mathf.PI) * AngleRad;
        if (type == "punch" && canShoot)
        {
            //Spawn Punch Block Here
            GameObject temp = GameObject.Instantiate(glove, this.playerObject.transform.position + new Vector3(Mathf.Cos(AngleRad), Mathf.Sin(AngleRad), 0), Quaternion.identity); //spawn at location of projectile with radius of 3 and angle applied
            temp.GetComponent<ProjectileCode>().angle = new Vector3(Mathf.Cos(AngleRad), Mathf.Sin(AngleRad), 0);
            temp.GetComponent<ProjectileCode>().owner = this.gameObject;
            temp.GetComponent<ProjectileCode>().speed = this.projectileSpeed;

            NetworkServer.Spawn(temp);

            StartCoroutine(StartPunchCountdown(2f));
        }
        if (type == "plunge" && canPlunge)
        {
            //Spawn Plunge Block Here 
            //Debug.Log("Plunge punch");
            GameObject temp = GameObject.Instantiate(plunger, this.playerObject.transform.position + new Vector3(Mathf.Cos(AngleRad), Mathf.Sin(AngleRad), 0), Quaternion.identity); //spawn at location of projectile with radius of 3 and angle applied
            temp.GetComponent<ProjectileCode>().angle = new Vector3(Mathf.Cos(AngleRad), Mathf.Sin(AngleRad), 0);
            temp.GetComponent<ProjectileCode>().owner = this.gameObject;
            temp.GetComponent<ProjectileCode>().speed = this.projectileSpeed;
            NetworkServer.Spawn(temp);


            StartCoroutine(StartPlungeCountdown(1));
        }
    }
    private void heartInsta(){
        health1 = GameObject.Find(this.gameObject.name + "Health1");
        health2 = GameObject.Find(this.gameObject.name + "Health2");
        health3 = GameObject.Find(this.gameObject.name + "Health3");

        health1.AddComponent<SpriteRenderer>();
        health2.AddComponent<SpriteRenderer>();
        health3.AddComponent<SpriteRenderer>();

        health1.GetComponent<SpriteRenderer>().sprite = hearts;

        health2.GetComponent<SpriteRenderer>().sprite = hearts;

        health3.GetComponent<SpriteRenderer>().sprite = hearts;
    }
    [ClientRpc]
    private void Rpc_heartInsta(){
        if(isClient){
            /* 
                This is where you will add the hearts, and then they will be taken away when the player loses them.
                The hearts will not show up if there aren't players to fill them, but the UI will keep the dark hearts for now
                We can discuss whether or not to change that.
            */

            health1 = GameObject.Find(this.gameObject.name + "Health1");
            health2 = GameObject.Find(this.gameObject.name + "Health2");
            health3 = GameObject.Find(this.gameObject.name + "Health3");

            health1.AddComponent<SpriteRenderer>();
            health2.AddComponent<SpriteRenderer>();
            health3.AddComponent<SpriteRenderer>();

            health1.GetComponent<SpriteRenderer>().sprite = hearts;

            health2.GetComponent<SpriteRenderer>().sprite = hearts;

            health3.GetComponent<SpriteRenderer>().sprite = hearts;
        }
    }
    [Command]
    public void Cmd_RespawnPlayer()
    {
        Debug.Log(characterIndex);
        //this.respawning = false;
        if (spawnPoints != null && spawnPoints.Length > 0 && this.playerObject == null && health > 0 && characterIndex != -1)
        {
            Debug.Log("Im spawning the player XD");
            /*
                I want to make this a pseudorandom spawn, to eliminate the spawning on top of eachother
                problem.

                This will help in making the player harder to spawn kill when respawning. 

            */
            int spawnLoc;
            if(this.gameObject.name == "player1"){
              spawnLoc = 0;
            }else if(this.gameObject.name == "player2"){
              spawnLoc = 1;
            }else if(this.gameObject.name == "player3"){
              spawnLoc = 2;
            }else{
              spawnLoc = 3;
            }
            Vector3 spawnPoint = spawnPoints[spawnLoc].transform.position;
            
            selectedPlayerPrefab = prefabCharacters[characterIndex];

            GameObject newPlayerObject = Instantiate(selectedPlayerPrefab, spawnPoint, Quaternion.identity);
            newPlayerObject.GetComponent<NetworkedPlayerMovement>().owner = this;
            newPlayerObject.GetComponent<NetworkedPlayerMovement>().ownerObject = this.gameObject;
            NetworkServer.Spawn(newPlayerObject);
            //Calling this to have everyone's hearts show up
            Rpc_heartInsta();
            heartInsta();
            //Debug.Log(this.gameObject.name);
            playerObject = selectedPlayerPrefab;
            canMove = true;
        }
    }
    
    void Start()
    {
        this.spawnPoints = FindObjectsOfType<NetworkStartPosition>();
        /* 
            This is where the players NetworkedPlayer obect is renamed so that each player can be easily identified within the game
            This was the best way to do this in my opinion. 
            When creating a new network engine, I will make this a standard thing to happen.
        */
        //respawning = true;
        currentMap = GameObject.Find("RoundTimer").GetComponent<MapTimers>();
        Timer = GameObject.Find("RoundTimer").GetComponent<Text>();
        //If this doesnt work, then Im gonna hard code this mother fucker
        Timer.text = "120";
        
        points = 0;
        if(GameObject.Find("player3")){
          this.gameObject.name = "player4";
          
        }
        else if(GameObject.Find("player2")){
          this.gameObject.name = "player3";
        } 
        else if(GameObject.Find("player1")){
          this.gameObject.name = "player2";
        }
        else{
          this.gameObject.name = "player1";
        }   
        /* 
            This is where the game finds out the player number of each player
            Once the game knows, it will assign each player a place in the corners.
         */
        playerNameText = GameObject.Find(this.gameObject.name + "Name").GetComponent<Text>();  
        playerScoreText = GameObject.Find(this.gameObject.name + "Score").GetComponent<Text>();
        health1 = GameObject.Find(this.gameObject.name + "Health1");
        health2 = GameObject.Find(this.gameObject.name + "Health2");
        health3 = GameObject.Find(this.gameObject.name + "Health3");  
       
        
        playerNameText.resizeTextForBestFit = true;
        playerNameText.color = Color.white;
        playerNameText.text = playerName;
        
        playerScoreText.color = Color.white;
        playerScoreText.fontSize = 32;
        playerScoreText.text = points.ToString();
        /*
            I will want an option to make the game on a timer...5 minutes... and will go purely off score, that way we can work with respawning, which will then be random.
        */
        canPlay = true;
        respawning = true;
        if(isServer){
            Cmd_RespawnPlayer();
        }
        
        
    }
    /* The below methods are for just dealing with the character visuals, am working on making this way better atm, so WIP*** */
    [Command]
    void Cmd_changeAnimation(int AnimChange){
        if (playerObject != null)
        {
            playerObject.GetComponent<NetworkAnimator>().GetComponent<Animator>().SetInteger("animationChanger", animationChanger);
        }
    }
    void Flip()
    {
        facingLeft = !facingLeft;
        playerObject.GetComponent<SpriteRenderer>().flipX = facingLeft;
        this.RpcFlip(facingLeft);
        Debug.Log("Flipping the player");
    }
    [ClientRpc]
    public void RpcFlip(bool b)
    {
        if(isClient){
            playerObject.GetComponent<SpriteRenderer>().flipX = b;
        }
    }
    private void animateChange(int newAnim){
		playerObject.GetComponent<Animator>().SetInteger("animationChanger", newAnim);
	}
    void Update()
    {
        //This will update when the points change and when the health drops.
        if(playerScoreText){
            playerScoreText.text = points.ToString();
        }
        else{
            playerScoreText = GameObject.Find(playerObject.name + "Score").GetComponent<Text>();
        }
        Timer.text = timeInGame.ToString();
        if (isServer)
        {
         
        }
        if (isClient)
        {
            
        }
        if (isLocalPlayer)
        {
            if (canPlay && respawning && playerObject == null)
            {
                //this.respawning = false;
                //this.Cmd_RespawnPlayer();
            }
            else
            {
                
                Cmd_SendMovements(Input.GetAxis("Horizontal"), (Input.GetAxis("Jump") > 0), Camera.main.ScreenToWorldPoint(Input.mousePosition));
                if (Input.GetAxis("Fire1") > 0 && this.canShoot)
                {
                    Cmd_SendShot(Camera.main.ScreenToWorldPoint(Input.mousePosition), "punch");
                }
                if (Input.GetAxis("Fire2") > 0 && this.canPlunge)
                {
                    Cmd_SendShot(Camera.main.ScreenToWorldPoint(Input.mousePosition), "plunge");
                }
                
            }
            if(playerObject){
                if (Input.GetAxis("Horizontal") < 0)
                {
                    if (facingLeft){
                        Flip();
                    }
                }
                else if(Input.GetAxis("Horizontal") > 0)
                {
                    if (!facingLeft)
                    {
                        Flip();
                    }
                }
        	    if (Input.GetAxis("Jump") > 0)
                {
                    //Set Animation to Jump
                    animateChange(2);

                }
                else if (Input.GetAxis("Horizontal") != 0)
                {
                    //Set Animation to the run animation, this will lose priority to the Jump Animation
                    animateChange(1);
                }
                else if(Input.GetKey("t")){
                    //TODO: Make sure your arms sprite render is disabled...*In Progress Below*
                    animateChange(3);
                    Debug.Log("Taunt");
                }
                else
                {
                    animateChange(0);
                }
            }
            /* 
		    if(playerObject.GetComponent<Animator>().GetInteger("animationChanger") == 3 && 
                playerObject.GetComponentInChildren<GameObject>().GetComponentInChildren<SpriteRenderer>().enabled)
            {
			    playerObject.GetComponentInChildren<GameObject>().GetComponentInChildren<SpriteRenderer>().enabled = false;
		    }
		    else if (!this.GetComponentInChildren<GameObject>().GetComponentInChildren<SpriteRenderer>().enabled)
            {
			    playerObject.GetComponentInChildren<GameObject>().GetComponentInChildren<SpriteRenderer>().enabled = true;
		    } 
            */
        }

        //This is extremely experimental, will need to talk about this, but will make it so players dont hit eachother, which is better
        //for when you plunge your opponent when far away and they done push you, but bad for when they are close and they go flying away...


        if (GameObject.Find("player1"))
        {
            Physics2D.IgnoreCollision(playerObject.GetComponent<BoxCollider2D>(), GameObject.Find("player1").GetComponent<NetworkedPlayer>().playerObject.GetComponent<BoxCollider2D>(), true);
        }
        else if (GameObject.Find("player2"))
        {
            Physics2D.IgnoreCollision(playerObject.GetComponent<BoxCollider2D>(), GameObject.Find("player2").GetComponent<NetworkedPlayer>().playerObject.GetComponent<BoxCollider2D>(), true);
        }
        else if (GameObject.Find("player3"))
        {
            Physics2D.IgnoreCollision(playerObject.GetComponent<BoxCollider2D>(), GameObject.Find("player3").GetComponent<NetworkedPlayer>().playerObject.GetComponent<BoxCollider2D>(), true);
        }
        else if (GameObject.Find("player4"))
        {
            Physics2D.IgnoreCollision(playerObject.GetComponent<BoxCollider2D>(), GameObject.Find("player4").GetComponent<NetworkedPlayer>().playerObject.GetComponent<BoxCollider2D>(), true);
        }
    }
}
