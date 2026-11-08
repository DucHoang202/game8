using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Self-bootstrapping Eclipse Frontier vertical slice. Open any scene and press Play.</summary>
public sealed class EclipseFrontierGame : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (FindAnyObjectByType<EclipseFrontierGame>() == null)
            new GameObject("Eclipse Frontier").AddComponent<EclipseFrontierGame>();
    }

    private enum GameScreen { Explore, Combat, Results }
    private sealed class Member { public string Name; public string Role; public float Essence; public float Capacity; public Color Tint; }
    private sealed class Enemy { public string Name; public string Title; public float Max; public float Essence; public float Resistance; public string Behavior; public Color Tint; }

    private readonly List<Member> party = new List<Member>();
    private GameScreen screen = GameScreen.Explore;
    private Enemy enemy;
    private float timer, timerMax, collected;
    private float disruption;
    private string combatLog = "The forest hums with latent Essence.";
    private string resultTitle = "Encounter complete";
    private string resultBody = "";
    private int level = 1, encounters, clones;
    private float exp, gold, legacy;
    private Vector3 playerPosition = new Vector3(0, 1.12f, -3);
    private readonly Vector3 bossArenaCenter = new Vector3(100, 0, 0);
    private Transform player, worldRoot, flask, runeBlade, bossRoot;
    private Camera cam;
    private InputAction moveAction;
    private GUIStyle titleStyle, labelStyle, mutedStyle, buttonStyle, smallStyle;
    private Texture2D pixel;
    private bool initialized;
    private float pulse;
    private float flaskVisibleUntil;
    private bool actionAnimating;

    private void Awake()
    {
        if (initialized) return;
        initialized = true;
        Application.targetFrameRate = 60;
        ConfigureMovementInput();
        LoadGame();
        BuildWorld();
    }

    private void OnDestroy()
    {
        if(moveAction!=null) { moveAction.Disable(); moveAction.Dispose(); }
    }

    private void ConfigureMovementInput()
    {
        moveAction=new InputAction("Move",InputActionType.Value);
        moveAction.AddCompositeBinding("2DVector")
            .With("Up","<Keyboard>/w").With("Down","<Keyboard>/s")
            .With("Left","<Keyboard>/a").With("Right","<Keyboard>/d");
        moveAction.AddCompositeBinding("2DVector")
            .With("Up","<Keyboard>/upArrow").With("Down","<Keyboard>/downArrow")
            .With("Left","<Keyboard>/leftArrow").With("Right","<Keyboard>/rightArrow");
        moveAction.AddBinding("<Gamepad>/leftStick");
        moveAction.Enable();
    }

    private void Update()
    {
        pulse += Time.deltaTime;
        if (flask != null && player != null)
        {
            bool drinking = Time.time < flaskVisibleUntil;
            flask.localScale = drinking ? new Vector3(.12f,.27f,.12f) : Vector3.zero;
            if (drinking)
            {
                // Keep the prop at Mint's mouth; alternate a short sip with a quick shake.
                float phase = (Time.time * 2.4f) % 1f;
                bool sip = phase < .58f;
                float shake = Mathf.Sin(Time.time * 34f) * (sip ? 4f : 15f);
                float tilt = sip ? 48f : 12f;
                flask.localPosition = new Vector3(.16f + Mathf.Sin(Time.time * 34f) * (sip ? .012f : .04f), 1.0f, .18f);
                flask.localRotation = Quaternion.Euler(tilt, 0, shake);
            }
            else
            {
                flask.localPosition = new Vector3(.52f,.18f,.02f);
                flask.localRotation = Quaternion.identity;
            }
        }
        bool roaming = screen == GameScreen.Explore || (screen == GameScreen.Combat && enemy != null && enemy.Title == "Eclipse Boss");
        if (!roaming || player == null) return;
        Vector3 move=Vector3.zero;
        if(!actionAnimating)
        {
            Vector2 moveValue=moveAction!=null ? moveAction.ReadValue<Vector2>() : Vector2.zero;
            move = new Vector3(moveValue.x, 0, moveValue.y).normalized;
            playerPosition += move * (4.2f * Time.deltaTime);
            if (screen == GameScreen.Combat)
            {
                playerPosition.x = Mathf.Clamp(playerPosition.x, bossArenaCenter.x - 13, bossArenaCenter.x + 13);
                playerPosition.z = Mathf.Clamp(playerPosition.z, bossArenaCenter.z - 13, bossArenaCenter.z + 13);
            }
            else
            {
                playerPosition.x = Mathf.Clamp(playerPosition.x, -18, 18);
                playerPosition.z = Mathf.Clamp(playerPosition.z, -14, 18);
            }
            if (move.sqrMagnitude > .01f) player.rotation = Quaternion.LookRotation(move);
        }
        player.position = playerPosition;
        if (cam != null)
        {
            Vector3 target = player.position + new Vector3(0, 5.8f, -8.5f);
            cam.transform.position = Vector3.Lerp(cam.transform.position, target, Time.deltaTime * 3f);
            cam.transform.LookAt(player.position + Vector3.up * 1.1f);
        }
    }

    private void BuildWorld()
    {
        if (Camera.main == null)
        {
            var cameraObject = new GameObject("Main Camera"); cameraObject.tag = "MainCamera";
            cam = cameraObject.AddComponent<Camera>();
        }
        else cam = Camera.main;
        cam.fieldOfView = 55; cam.backgroundColor = new Color(.055f, .08f, .12f);
        cam.transform.position = new Vector3(0, 7, -11); cam.transform.rotation = Quaternion.Euler(25, 0, 0);
        RenderSettings.ambientLight = new Color(.48f, .48f, .57f);
        RenderSettings.fog = true; RenderSettings.fogColor = new Color(.12f, .17f, .2f); RenderSettings.fogDensity = .012f;
        var lightObject = new GameObject("Moonlight"); var light = lightObject.AddComponent<Light>();
        light.type=LightType.Directional; light.intensity=1.25f; light.color=new Color(.76f,.79f,1f);
        lightObject.transform.rotation = Quaternion.Euler(42, -32, 0);
        worldRoot = new GameObject("Generated Moonfall Forest").transform;
        MakePrimitive(PrimitiveType.Plane, "Mossy clearing", new Vector3(0, -.12f, 1), new Vector3(4, 1, 4), new Color(.14f, .23f, .2f), worldRoot);
        // Winding pale path
        for (int i = 0; i < 11; i++)
        {
            float z = -10 + i * 2.5f; float x = Mathf.Sin(i * .46f) * 1.8f;
            MakePrimitive(PrimitiveType.Cube, "Moonlit path", new Vector3(x, -.035f, z), new Vector3(3.5f, .06f, 2.25f), new Color(.34f, .35f, .31f), worldRoot);
        }
        var rng = new System.Random(24);
        for (int i = 0; i < 62; i++)
        {
            float x = (float)rng.NextDouble() * 40 - 20, z = (float)rng.NextDouble() * 40 - 15;
            if (Mathf.Abs(x) < 3 && z < 12) continue;
            float height = 2.5f + (float)rng.NextDouble() * 2.7f;
            MakePrimitive(PrimitiveType.Cylinder, "Ancient tree", new Vector3(x, height * .45f, z), new Vector3(.2f, height * .45f, .2f), new Color(.14f, .12f, .14f), worldRoot);
            var crown = MakePrimitive(PrimitiveType.Sphere, "Canopy", new Vector3(x, height, z), new Vector3(1.1f, 1.8f, 1.1f), i % 4 == 0 ? new Color(.28f, .18f, .37f) : new Color(.1f, .22f, .2f), worldRoot);
        }
        for (int i = 0; i < 16; i++)
        {
            float x = Mathf.Sin(i * 7.1f) * 13, z = Mathf.Cos(i * 3.3f) * 13;
            var crystal = MakePrimitive(PrimitiveType.Sphere, "Essence crystal", new Vector3(x, .35f, z), new Vector3(.25f, .7f, .25f), new Color(.58f, .28f, .95f), worldRoot);
            crystal.transform.rotation = Quaternion.Euler(0, i * 31, 12);
        }
        player = new GameObject("Wayfarer").transform;
        player.SetParent(worldRoot); player.position=playerPosition;
        GameObject characterPrefab=Resources.Load<GameObject>("PlayerCharacter");
        if(characterPrefab!=null)
        {
            GameObject model=Instantiate(characterPrefab,player);
            model.name="Imported Main Character";
            model.transform.localPosition=Vector3.zero; model.transform.localRotation=Quaternion.identity; model.transform.localScale=Vector3.one;
            Renderer[] renderers=model.GetComponentsInChildren<Renderer>(true);
            if(renderers.Length>0)
            {
                Bounds bounds=renderers[0].bounds;
                for(int i=1;i<renderers.Length;i++) bounds.Encapsulate(renderers[i].bounds);
                float fit=2.24f/Mathf.Max(.01f,bounds.size.y);
                float localFootOffset=(bounds.min.y-player.position.y)*fit;
                model.transform.localScale=Vector3.one*fit;
                model.transform.localPosition=new Vector3(0,-1.12f-localFootOffset,0);
            }
        }
        else
        {
            var fallback=MakePrimitive(PrimitiveType.Capsule,"Wayfarer · fallback",playerPosition,new Vector3(.72f,1.12f,.72f),new Color(.45f,.75f,.85f),worldRoot);
            fallback.transform.SetParent(player,true); fallback.transform.localPosition=Vector3.zero;
            var collider=fallback.GetComponent<Collider>(); if(collider!=null)Destroy(collider);
        }
        MakePrimitive(PrimitiveType.Sphere, "Wayfarer aura", playerPosition + Vector3.up * 1.1f, new Vector3(.2f,.2f,.2f), new Color(.7f,.4f,1), player);
        flask = MakePrimitive(PrimitiveType.Cylinder, "Empty flask", playerPosition + new Vector3(.55f,.3f,.05f), Vector3.zero, new Color(.25f,.77f,.85f), player).transform;
        flask.localPosition = new Vector3(.52f,.18f,.02f);
        runeBlade = MakePrimitive(PrimitiveType.Cylinder,"Wayfarer · rune blade",playerPosition,Vector3.zero,new Color(.72f,.9f,1f),player).transform;
        runeBlade.localPosition=new Vector3(.52f,.25f,.35f);
        // Three distant encounter landmarks
        MakeLandmark("Gloamling", new Vector3(-8, .8f, 10), new Color(.63f,.32f,.82f));
        MakeLandmark("Moss Warden", new Vector3(8, 1.1f, 15), new Color(.3f,.65f,.46f));
        MakeLandmark("Hollow Stag", new Vector3(0, 1, 23), new Color(.8f,.6f,.32f));
        BuildBossArena();
        player.position = playerPosition;
    }

    private void BuildBossArena()
    {
        Vector3 c=bossArenaCenter;
        MakePrimitive(PrimitiveType.Plane,"Hollow Stag arena",c+Vector3.down*.12f,new Vector3(3,1,3),new Color(.15f,.16f,.2f),worldRoot);
        for(int i=0;i<8;i++)
        {
            float angle=i*Mathf.PI/4f; Vector3 p=c+new Vector3(Mathf.Cos(angle)*12,2.2f,Mathf.Sin(angle)*12);
            MakePrimitive(PrimitiveType.Cylinder,"Ruined arena pillar",p,new Vector3(.7f,2.2f,.7f),new Color(.25f,.22f,.3f),worldRoot);
            MakePrimitive(PrimitiveType.Sphere,"Violet ward",p+Vector3.up*2.1f,new Vector3(.22f,.22f,.22f),new Color(.64f,.28f,.94f),worldRoot);
        }
        Vector3 b=c+new Vector3(0,1.25f,6.5f);
        bossRoot=new GameObject("Hollow Stag boss rig").transform; bossRoot.SetParent(worldRoot); bossRoot.position=b;
        var body=MakePrimitive(PrimitiveType.Capsule,"Hollow Stag · body",b,new Vector3(1.15f,1.4f,2.4f),new Color(.45f,.35f,.2f),bossRoot);
        body.transform.rotation=Quaternion.Euler(90,0,0);
        MakePrimitive(PrimitiveType.Sphere,"Hollow Stag · chest",b+new Vector3(0,.55f,-1.45f),new Vector3(.9f,1.05f,.95f),new Color(.55f,.43f,.26f),bossRoot);
        MakePrimitive(PrimitiveType.Sphere,"Hollow Stag · head",b+new Vector3(0,.75f,-2.25f),new Vector3(.65f,.7f,.7f),new Color(.58f,.45f,.27f),bossRoot);
        for(int side=-1;side<=1;side+=2)
        {
            Vector3 antler=b+new Vector3(side*.38f,1.35f,-2.3f);
            var tine=MakePrimitive(PrimitiveType.Cylinder,"Moon antler",antler+new Vector3(side*.18f,.5f,0),new Vector3(.09f,.65f,.09f),new Color(.72f,.65f,.47f),bossRoot);
            tine.transform.rotation=Quaternion.Euler(0,0,-side*18);
            var tine2=MakePrimitive(PrimitiveType.Cylinder,"Moon antler tine",antler+new Vector3(side*.48f,.72f,0),new Vector3(.07f,.4f,.07f),new Color(.72f,.65f,.47f),bossRoot);
            tine2.transform.rotation=Quaternion.Euler(0,0,-side*48);
        }
        for(int side=-1;side<=1;side+=2) for(int front=-1;front<=1;front+=2)
            MakePrimitive(PrimitiveType.Cylinder,"Stag leg",b+new Vector3(side*.65f,-.75f,front*1.25f),new Vector3(.18f,.85f,.18f),new Color(.35f,.28f,.19f),bossRoot);
        MakePrimitive(PrimitiveType.Cylinder,"Boss arena sigil",c+Vector3.up*.015f,new Vector3(5,.03f,5),new Color(.34f,.17f,.46f),worldRoot);
    }

    private void MakeLandmark(string name, Vector3 pos, Color color)
    {
        var body = MakePrimitive(PrimitiveType.Sphere, name, pos, new Vector3(1.25f,1.5f,1.05f), color, worldRoot);
        body.transform.localScale = new Vector3(1.25f,1.5f,1.05f);
        var ring = MakePrimitive(PrimitiveType.Cylinder, "Encounter sigil", pos + Vector3.down * .7f, new Vector3(1.2f,.04f,1.2f), new Color(.47f,.25f,.72f), worldRoot);
    }
    private GameObject MakePrimitive(PrimitiveType type, string name, Vector3 pos, Vector3 scale, Color color, Transform parent)
    {
        var go = GameObject.CreatePrimitive(type); go.name=name; go.transform.SetParent(parent); go.transform.position=pos; go.transform.localScale=scale;
        var r=go.GetComponent<Renderer>(); r.material = new Material(Shader.Find("Standard")); r.material.color=color;
        if (color.r > .45f && color.b > .5f) { r.material.EnableKeyword("_EMISSION"); r.material.SetColor("_EmissionColor", color * .6f); }
        var collider=go.GetComponent<Collider>(); if (collider != null && parent == player) Destroy(collider);
        return go;
    }

    private void EnsureStyles()
    {
        if (titleStyle != null) return;
        pixel = new Texture2D(1,1); pixel.SetPixel(0,0,Color.white); pixel.Apply();
        titleStyle = new GUIStyle(GUI.skin.label) { fontSize=25, fontStyle=FontStyle.Bold, alignment=TextAnchor.MiddleLeft };
        titleStyle.normal.textColor = new Color(.94f,.91f,1);
        labelStyle = new GUIStyle(GUI.skin.label) { fontSize=14, wordWrap=true }; labelStyle.normal.textColor=new Color(.87f,.87f,.93f);
        mutedStyle = new GUIStyle(labelStyle) { fontSize=11 }; mutedStyle.normal.textColor=new Color(.61f,.65f,.73f);
        smallStyle = new GUIStyle(labelStyle) { fontSize=12 }; smallStyle.normal.textColor=new Color(.76f,.72f,.86f);
        buttonStyle = new GUIStyle(GUI.skin.button) { fontSize=14, fontStyle=FontStyle.Bold, alignment=TextAnchor.MiddleCenter, wordWrap=true };
        buttonStyle.normal.textColor=Color.white; buttonStyle.hover.textColor=new Color(.8f,.7f,1);
    }

    private void OnGUI()
    {
        EnsureStyles();
        float s=Mathf.Min(UnityEngine.Screen.width/1440f, UnityEngine.Screen.height/900f); if(s<.65f) s=.65f;
        GUI.matrix=Matrix4x4.Scale(new Vector3(s,s,1));
        float w=UnityEngine.Screen.width/s, h=UnityEngine.Screen.height/s;
        DrawTopbar(w);
        if(screen==GameScreen.Explore) DrawExplore(w,h);
        else if(screen==GameScreen.Combat) DrawCombat(w,h);
        else DrawResults(w,h);
    }
    private void Box(Rect r, Color c) { GUI.color=c; GUI.DrawTexture(r,pixel); GUI.color=Color.white; }
    private void Text(Rect r,string text,GUIStyle style) { GUI.Label(r,text,style); }
    private void DrawTopbar(float w)
    {
        Box(new Rect(0,0,w,66),new Color(.055f,.055f,.09f,.94f));
        Text(new Rect(28,10,340,28),"ECLIPSE  /  FRONTIER",titleStyle);
        Text(new Rect(30,39,340,18),"MOONFALL FOREST     •     REGION 01",mutedStyle);
        Text(new Rect(w-430,13,390,23),$"WAYFARER   LV {level}      ✦  {Mathf.FloorToInt(gold)} GOLD      ◈  {Mathf.FloorToInt(legacy)} LEGACY",smallStyle);
        Box(new Rect(0,65,w,1),new Color(.49f,.32f,.7f,.65f));
    }
    private void DrawExplore(float w,float h)
    {
        // unobtrusive title overlay, encounter cards
        Box(new Rect(28,88,310,92),new Color(.035f,.045f,.075f,.8f));
        Text(new Rect(46,99,270,28),"THE VEILWOOD",titleStyle);
        Text(new Rect(47,132,270,38),"A quiet path beneath an eclipsed moon.",mutedStyle);
        DrawPartyCard(new Rect(28,h-204,340,174));
        float cw=300; float x=w-cw-28;
        Box(new Rect(x,88,cw,300),new Color(.035f,.045f,.075f,.9f));
        Text(new Rect(x+20,103,cw-40,26),"FIELD ENCOUNTERS",new GUIStyle(titleStyle){fontSize=18});
        Text(new Rect(x+20,132,cw-40,36),"Choose a presence in the forest to begin an encounter.",mutedStyle);
        EncounterButton(x+18,179,cw-36,"01   Gloamling", "Wisp • 420 Essence", 0);
        EncounterButton(x+18,240,cw-36,"02   Moss Warden", "Elite • 760 Essence", 1);
        EncounterButton(x+18,301,cw-36,"03   Hollow Stag", "Boss • 1,250 Essence", 2);
        Text(new Rect(x+16,402,cw-32,32),$"ENCOUNTERS COMPLETED   {encounters}     •     MINI CLONES   {clones}",mutedStyle);
        Box(new Rect(w/2-150,h-54,300,34),new Color(.04f,.05f,.08f,.75f));
        Text(new Rect(w/2-140,h-50,280,26),"WASD / ARROWS / LEFT STICK  ·  MOVE",mutedStyle);
        if(clones==0 && legacy>=100) Button(new Rect(w-338,h-113,310,46),"CREATE MINI CLONE  ·  100 LEGACY",CreateClone,new Color(.37f,.2f,.55f));
    }
    private void EncounterButton(float x,float y,float width,string name,string desc,int index)
    {
        Box(new Rect(x,y,width,52),new Color(.14f,.12f,.2f,.93f));
        Text(new Rect(x+12,y+4,width-24,21),name,labelStyle);
        Text(new Rect(x+12,y+27,width-24,18),desc,mutedStyle);
        if(GUI.Button(new Rect(x,y,width,52),GUIContent.none,GUIStyle.none)) StartEncounter(index);
    }
    private void DrawPartyCard(Rect r)
    {
        Box(r,new Color(.035f,.045f,.075f,.88f)); Text(new Rect(r.x+16,r.y+11,300,24),"YOUR PARTY",new GUIStyle(titleStyle){fontSize=17});
        for(int i=0;i<party.Count;i++)
        {
            var m=party[i]; float y=r.y+46+i*29; Text(new Rect(r.x+17,y,165,20),m.Name+"  ·  "+m.Role,smallStyle);
            Box(new Rect(r.x+185,y+5,125,9),new Color(.18f,.18f,.23f)); Box(new Rect(r.x+185,y+5,125*m.Essence/m.Capacity,9),new Color(.55f,.32f,.79f));
            Text(new Rect(r.x+313,y,25,18),$"{m.Essence:0}",mutedStyle);
        }
    }
    private void DrawCombat(float w,float h)
    {
        if(enemy.Title=="Eclipse Boss") { DrawBossCombat(w,h); return; }
        Box(new Rect(0,66,w,h-66),new Color(.025f,.025f,.05f,.71f));
        float pw=760, px=(w-pw)/2, py=95;
        Box(new Rect(px,py,pw,120),new Color(.045f,.05f,.085f,.96f));
        Text(new Rect(px+24,py+11,pw-48,26),enemy.Title.ToUpper()+"  /  "+enemy.Name.ToUpper(),new GUIStyle(titleStyle){fontSize=21,alignment=TextAnchor.MiddleCenter});
        Text(new Rect(px+25,py+45,pw-50,22),$"ENEMY ESSENCE     {enemy.Essence:0}  /  {enemy.Max:0}",smallStyle);
        Bar(new Rect(px+25,py+72,pw-50,13),enemy.Essence/enemy.Max,new Color(.62f,.34f,.83f));
        Text(new Rect(px+25,py+91,pw-50,19),$"BEHAVIOR   {enemy.Behavior}     •     EXTRACTION RESISTANCE   {enemy.Resistance:P0}",mutedStyle);
        float left=px, right=px+pw-250;
        Box(new Rect(left,py+136,250,105),new Color(.045f,.05f,.085f,.95f));
        Text(new Rect(left+16,py+145,215,22),"EXTRACTION TIMER",smallStyle);
        Text(new Rect(left+16,py+169,215,37),$"{timer:0.0}s",new GUIStyle(titleStyle){fontSize=28});
        Bar(new Rect(left+16,py+211,215,10),timer/timerMax,new Color(.95f,.47f,.28f));
        Box(new Rect(right,py+136,250,105),new Color(.045f,.05f,.085f,.95f));
        Text(new Rect(right+16,py+145,215,22),"TEAM RESERVOIR",smallStyle);
        float cap=TotalCapacity(), used=TotalEssence();
        Text(new Rect(right+16,py+169,215,31),$"{used:0}  /  {cap:0}",new GUIStyle(titleStyle){fontSize=23});
        Bar(new Rect(right+16,py+211,215,10),used/cap,new Color(.25f,.73f,.68f));
        float cardY=py+257;
        for(int i=0;i<party.Count;i++)
        {
            var m=party[i]; float x=px+i*188;
            Box(new Rect(x,cardY,177,78),new Color(.045f,.05f,.085f,.95f));
            Text(new Rect(x+12,cardY+8,155,20),m.Name,smallStyle);
            Text(new Rect(x+12,cardY+31,155,18),$"RESERVOIR   {m.Essence:0} / {m.Capacity:0}",mutedStyle);
            Bar(new Rect(x+12,cardY+56,153,8),m.Essence/m.Capacity,new Color(.55f,.32f,.79f));
        }
        float ay=cardY+98, bw=232, gap=20, start=px+(pw-(bw*3+gap*2))/2;
        ActionButton(new Rect(start,ay,bw,70),"DRINK & SHAKE",$"10 sec  ·  sip from the flask while shaking",DrinkAndExtract,new Color(.33f,.19f,.47f));
        bool isBoss=enemy.Title=="Eclipse Boss";
        Action burstAction;
        if(isBoss) burstAction=MountedStrike;
        else burstAction=()=>Extract(2.4f,15f);
        ActionButton(new Rect(start+bw+gap,ay,bw,70),isBoss?"AERIAL RUNE STRIKE":"BURST EXTRACTION",isBoss?"15 sec  ·  leap onto armor, strike a rune":"15 sec  ·  extract ×2.4",burstAction,new Color(.48f,.2f,.28f));
        ActionButton(new Rect(start+(bw+gap)*2,ay,bw,70),"GROUNDING SHOVE",$"3 sec  ·  break stance, gain 5 sec",GroundingShove,new Color(.22f,.26f,.38f));
        Box(new Rect(px,ay+82,pw,48),new Color(.035f,.04f,.07f,.91f));
        Text(new Rect(px+14,ay+90,pw-28,20),"FIELD LOG   /   "+combatLog,mutedStyle);
        if(enemy.Title=="Eclipse Boss") Text(new Rect(px+14,ay+111,pw-28,18),"WASD / ARROWS / LEFT STICK  ·  RUN THROUGH THE ARENA",mutedStyle);
        Button(new Rect(w-190,h-48,165,30),"RETREAT",()=>Finish(false),new Color(.24f,.18f,.2f));
    }
    private void DrawBossCombat(float w,float h)
    {
        float x=22,y=82;
        Box(new Rect(x,y,322,126),new Color(.035f,.04f,.07f,.82f));
        Text(new Rect(x+14,y+8,294,22),"ECLIPSE BOSS  ·  HOLLOW STAG",smallStyle);
        Text(new Rect(x+14,y+32,294,22),$"ESSENCE   {enemy.Essence:0} / {enemy.Max:0}",labelStyle);
        Bar(new Rect(x+14,y+57,294,9),enemy.Essence/enemy.Max,new Color(.65f,.32f,.84f));
        Text(new Rect(x+14,y+72,150,20),$"TIMER   {timer:0}s",smallStyle);
        Bar(new Rect(x+14,y+99,294,9),timer/timerMax,new Color(.95f,.47f,.28f));
        Text(new Rect(x+169,y+72,139,20),$"RESIST   {enemy.Resistance:P0}",mutedStyle);

        float right=w-306, ry=82, rh=57+party.Count*26;
        Box(new Rect(right,ry,284,rh),new Color(.035f,.04f,.07f,.82f));
        Text(new Rect(right+13,ry+8,258,20),$"TEAM   {TotalEssence():0} / {TotalCapacity():0}",smallStyle);
        for(int i=0;i<party.Count;i++)
        {
            Member m=party[i]; float cy=ry+32+i*26;
            Text(new Rect(right+13,cy,104,18),m.Name,mutedStyle);
            Bar(new Rect(right+118,cy+4,151,8),m.Essence/m.Capacity,new Color(.55f,.32f,.79f));
        }

        float panelWidth=760, panelX=(w-panelWidth)/2, actionY=h-104, bw=232, gap=20;
        Box(new Rect(panelX,h-139,panelWidth,28),new Color(.035f,.04f,.07f,.68f));
        Text(new Rect(panelX+10,h-136,panelWidth-20,22),combatLog,mutedStyle);
        ActionButton(new Rect(panelX,actionY,bw,66),"DRINK & SHAKE","10 sec  ·  sip while shaking the flask",DrinkAndExtract,new Color(.33f,.19f,.47f,.92f));
        ActionButton(new Rect(panelX+bw+gap,actionY,bw,66),"AERIAL RUNE STRIKE","15 sec  ·  leap onto armor, strike rune",MountedStrike,new Color(.48f,.2f,.28f,.92f));
        ActionButton(new Rect(panelX+(bw+gap)*2,actionY,bw,66),"GROUNDING SHOVE","3 sec  ·  break stance, gain 5 sec",GroundingShove,new Color(.22f,.26f,.38f,.92f));
        Button(new Rect(w-190,h-35,165,26),"RETREAT",()=>Finish(false),new Color(.24f,.18f,.2f,.88f));
    }
    private void Bar(Rect r,float ratio,Color color) { Box(r,new Color(.15f,.15f,.2f)); Box(new Rect(r.x,r.y,r.width*Mathf.Clamp01(ratio),r.height),color); }
    private void ActionButton(Rect r,string title,string description,Action action,Color color)
    {
        Box(r,color); Text(new Rect(r.x+5,r.y+8,r.width-10,23),title,new GUIStyle(labelStyle){fontStyle=FontStyle.Bold,alignment=TextAnchor.MiddleCenter});
        Text(new Rect(r.x+8,r.y+34,r.width-16,31),description,new GUIStyle(mutedStyle){alignment=TextAnchor.MiddleCenter});
        if(GUI.Button(r,GUIContent.none,GUIStyle.none)) action();
    }
    private void DrawResults(float w,float h)
    {
        Box(new Rect(0,66,w,h-66),new Color(.025f,.025f,.05f,.78f)); float x=w/2-285,y=h/2-190;
        Box(new Rect(x,y,570,380),new Color(.055f,.055f,.1f,.98f));
        Text(new Rect(x+34,y+28,502,40),resultTitle.ToUpper(),new GUIStyle(titleStyle){alignment=TextAnchor.MiddleCenter});
        Text(new Rect(x+55,y+91,460,55),resultBody,new GUIStyle(labelStyle){alignment=TextAnchor.MiddleCenter,fontSize=16});
        Text(new Rect(x+70,y+171,430,25),$"ESSENCE EXTRACTED     {collected:0}",smallStyle);
        Text(new Rect(x+70,y+201,430,25),$"NORMAL REWARDS     +{Mathf.FloorToInt(collected*.7f)} GOLD  ·  +{Mathf.FloorToInt(collected*.12f)} EXP",smallStyle);
        Text(new Rect(x+70,y+231,430,25),$"LEGACY ESSENCE     +{Mathf.FloorToInt(collected*.3f)}",smallStyle);
        Button(new Rect(x+100,y+292,370,50),"RETURN TO THE VEILWOOD",()=>
        {
            screen=GameScreen.Explore; playerPosition=new Vector3(0,1.12f,-3); if(player!=null)player.position=playerPosition; SaveGame();
        },new Color(.34f,.2f,.48f));
    }
    private void Button(Rect r,string text,Action act,Color color)
    { Box(r,color); if(GUI.Button(r,text,buttonStyle)) act(); }

    private void StartEncounter(int index)
    {
        string[] names={"Gloamling","Moss Warden","Hollow Stag"}, titles={"Veil Wisp","Ancient Sentinel","Eclipse Boss"}, behaviors={"Disrupt · weakens extraction", "Recovery · resistance rises", "Phase Shift · changing forms"};
        float[] max={420,760,1250}, resist={.08f,.18f,.24f};
        enemy=new Enemy { Name=names[index],Title=titles[index],Max=max[index],Essence=max[index],Resistance=resist[index],Behavior=behaviors[index] };
        collected=0; timerMax=75; timer=timerMax; disruption=0; combatLog="Encounter opened. Choose an action; the timer pauses while you decide.";
        encounters++; screen=GameScreen.Combat;
        if(player != null)
        {
            playerPosition = index==2 ? bossArenaCenter + new Vector3(0,1.12f,-7) : new Vector3(0,1.12f,0);
            player.position=playerPosition;
        }
    }
    private void Extract(float mult,float cost)
    {
        if(screen!=GameScreen.Combat || timer<=0) return;
        if(timer<cost) { resultTitle="Extraction window closed"; resultBody="There was not enough time to complete that action. Your gathered Essence remains yours."; Finish(false); return; }
        float output=92f*mult*(1-enemy.Resistance)*(1-disruption);
        float gain=Distribute(Mathf.Min(output,enemy.Essence));
        enemy.Essence-=gain; collected+=gain; timer-=cost;
        combatLog=$"Essence drawn: {gain:0}. {enemy.Name} has {enemy.Essence:0} Essence remaining.";
        disruption=0;
        if(enemy.Essence<=.01f) { enemy.Essence=0; resultTitle="Enemy depleted"; resultBody=$"{enemy.Name} dissolves into a stream of violet light."; Finish(true); }
        else if(TotalEssence()>=TotalCapacity()-.01f) { resultTitle="Capacity reached"; resultBody="Every reservoir is filled. The party secures the encounter."; Finish(true); }
        else EnemyTurn();
        SaveGame();
    }
    private void DrinkAndExtract()
    {
        flaskVisibleUntil=Time.time+2.4f;
        Extract(1f,10f);
        if(screen==GameScreen.Combat) combatLog="You hold the flask to your mouth, take quick sips, and shake it between them for the last drops.";
    }
    private void MountedStrike()
    {
        if(screen!=GameScreen.Combat || enemy==null || enemy.Title!="Eclipse Boss") return;
        if(timer<15f) { Extract(2.4f,15f); return; }
        StartCoroutine(MountedStrikeAnimation());
        Extract(2.4f,15f);
        if(screen==GameScreen.Combat) combatLog="You vault onto the guardian's armor and strike a rune as it topples onto its back.";
    }
    private IEnumerator MountedStrikeAnimation()
    {
        actionAnimating=true;
        Vector3 origin=player.position, target=bossRoot.position+new Vector3(0,2.05f,-.25f);
        bossRoot.localRotation=Quaternion.Euler(0,0,165f);
        float t=0;
        while(t<.38f)
        {
            t+=Time.deltaTime; float a=Mathf.Clamp01(t/.38f);
            player.position=Vector3.Lerp(origin,target,a)+Vector3.up*Mathf.Sin(a*Mathf.PI)*1.3f;
            player.rotation=Quaternion.LookRotation(Vector3.forward);
            yield return null;
        }
        runeBlade.localScale=new Vector3(.08f,.9f,.08f);
        runeBlade.localRotation=Quaternion.Euler(0,0,-55);
        yield return new WaitForSeconds(.65f);
        runeBlade.localScale=Vector3.zero;
        t=0;
        while(t<.34f)
        {
            t+=Time.deltaTime; float a=Mathf.Clamp01(t/.34f);
            player.position=Vector3.Lerp(target,origin,a)+Vector3.up*Mathf.Sin((1-a)*Mathf.PI)*.8f;
            yield return null;
        }
        player.position=origin; player.localScale=Vector3.one;
        bossRoot.localRotation=Quaternion.identity; actionAnimating=false;
    }
    private void GroundingShove()
    {
        if(!Spend(3))return;
        timer=Mathf.Min(timer+5,timerMax);
        StartCoroutine(GroundingShoveAnimation());
        combatLog="You drop into a low stance and shoulder-check forward, then recoil to break the boss's guard.";
        EnemyTurn(false);
    }
    private IEnumerator GroundingShoveAnimation()
    {
        actionAnimating=true;
        Vector3 origin=player.position;
        for(int i=0;i<3;i++)
        {
            float t=0;
            while(t<.18f) { t+=Time.deltaTime; player.position=origin+new Vector3(0,-.12f,.5f*Mathf.Clamp01(t/.18f)); yield return null; }
            t=0;
            while(t<.18f) { t+=Time.deltaTime; player.position=origin+new Vector3(0,-.12f,.5f*(1-Mathf.Clamp01(t/.18f))); yield return null; }
        }
        player.position=origin; actionAnimating=false;
    }
    private bool Spend(float cost)
    {
        if(screen!=GameScreen.Combat || timer<cost) { resultTitle="Time exhausted"; resultBody="The extraction window closed. Your gathered Essence remains yours."; Finish(false); return false; }
        timer-=cost; return true;
    }
    private void EnemyTurn(bool act=true)
    {
        if(act && enemy.Title=="Veil Wisp") disruption=.25f;
        if(act && enemy.Title=="Ancient Sentinel" && UnityEngine.Random.value<.35f) enemy.Resistance=Mathf.Min(.55f,enemy.Resistance+.06f);
        if(enemy.Title=="Eclipse Boss" && enemy.Essence<enemy.Max*.5f) enemy.Resistance=.32f;
        if(timer<=0) { resultTitle="Extraction window closed"; resultBody="The encounter ends. Your gathered Essence remains yours."; Finish(false); }
        SaveGame();
    }
    private float Distribute(float amount)
    {
        float remain=Mathf.Min(amount,TotalCapacity()-TotalEssence()), moved=0;
        while(remain>.01f)
        {
            Member best=null; float available=-1, fillRatio=float.MaxValue;
            foreach(var m in party)
            {
                float a=m.Capacity-m.Essence, ratio=m.Essence/m.Capacity;
                if(a>available+.001f || (Mathf.Abs(a-available)<=.001f && ratio<fillRatio-.0001f)) { best=m; available=a; fillRatio=ratio; }
            }
            if(best==null || available<=.001f) break;
            float part=Mathf.Min(remain,available); best.Essence+=part; remain-=part; moved+=part;
        }
        return moved;
    }
    private float TotalEssence(){float x=0;foreach(var m in party)x+=m.Essence;return x;}
    private float TotalCapacity(){float x=0;foreach(var m in party)x+=m.Capacity;return x;}
    private void Finish(bool victory)
    {
        screen=GameScreen.Results;
        float normal=Mathf.Floor(collected*.7f), old=Mathf.Floor(collected*.3f);
        gold+=normal; legacy+=old; exp+=collected*.12f;
        foreach(var m in party) m.Essence=0;
        while(exp>=100+level*30) {exp-=100+level*30;level++;}
        if(!victory && collected<=0) { normal=0;old=0; }
        SaveGame();
    }
    private void CreateClone()
    {
        if(clones>0 || legacy<100)return; legacy-=100; clones++;
        party.Add(new Member{Name="Mini Clone",Role="Replica",Capacity=180,Essence=0,Tint=new Color(.72f,.55f,1)});
        SaveGame();
    }
    private void LoadGame()
    {
        party.Add(new Member{Name="Ari",Role="Wayfarer",Capacity=240,Tint=new Color(.4f,.8f,.9f)});
        party.Add(new Member{Name="Kael",Role="Knight",Capacity=360,Tint=new Color(.4f,.65f,.9f)});
        party.Add(new Member{Name="Lyra",Role="Mage",Capacity=220,Tint=new Color(.8f,.55f,.95f)});
        if(!PlayerPrefs.HasKey("ef_save"))return;
        level=PlayerPrefs.GetInt("ef_level",1); encounters=PlayerPrefs.GetInt("ef_encounters",0); clones=PlayerPrefs.GetInt("ef_clones",0);
        gold=PlayerPrefs.GetFloat("ef_gold",0); legacy=PlayerPrefs.GetFloat("ef_legacy",0); exp=PlayerPrefs.GetFloat("ef_exp",0);
        if(clones>0)party.Add(new Member{Name="Mini Clone",Role="Replica",Capacity=180,Tint=new Color(.72f,.55f,1)});
    }
    private void SaveGame()
    {
        PlayerPrefs.SetInt("ef_save",1); PlayerPrefs.SetInt("ef_level",level); PlayerPrefs.SetInt("ef_encounters",encounters); PlayerPrefs.SetInt("ef_clones",clones);
        PlayerPrefs.SetFloat("ef_gold",gold); PlayerPrefs.SetFloat("ef_legacy",legacy); PlayerPrefs.SetFloat("ef_exp",exp); PlayerPrefs.Save();
    }
}
