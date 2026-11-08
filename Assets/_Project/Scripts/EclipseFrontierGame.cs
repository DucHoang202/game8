using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Self-bootstrapping Eclipse Frontier vertical slice. Open any scene and press Play.</summary>
public sealed class EclipseFrontierGame : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (FindObjectOfType<EclipseFrontierGame>() == null)
            new GameObject("Eclipse Frontier").AddComponent<EclipseFrontierGame>();
    }

    private enum GameScreen { Explore, Combat, Results }
    private sealed class Member { public string Name; public string Role; public float Essence; public float Capacity; public Color Tint; }
    private sealed class Enemy { public string Name; public string Title; public float Max; public float Essence; public float Resistance; public string Behavior; public Color Tint; }

    private readonly List<Member> party = new List<Member>();
    private GameScreen screen = GameScreen.Explore;
    private Enemy enemy;
    private float timer, timerMax, collected, actionMultiplier = 1f;
    private float disruption;
    private string combatLog = "The forest hums with latent Essence.";
    private string resultTitle = "Encounter complete";
    private string resultBody = "";
    private int level = 1, encounters, clones;
    private float exp, gold, legacy;
    private Vector3 playerPosition = new Vector3(0, 0, -3);
    private Transform player, worldRoot;
    private Camera cam;
    private GUIStyle titleStyle, labelStyle, mutedStyle, buttonStyle, smallStyle;
    private Texture2D pixel;
    private bool initialized;
    private float pulse;

    private void Awake()
    {
        if (initialized) return;
        initialized = true;
        Application.targetFrameRate = 60;
        LoadGame();
        BuildWorld();
    }

    private void Update()
    {
        pulse += Time.deltaTime;
        if (screen != GameScreen.Explore || player == null) return;
        float x = (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow) ? 1f : 0f) - (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow) ? 1f : 0f);
        float z = (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow) ? 1f : 0f) - (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow) ? 1f : 0f);
        Vector3 move = new Vector3(x, 0, z).normalized;
        playerPosition += move * (4.2f * Time.deltaTime);
        playerPosition.x = Mathf.Clamp(playerPosition.x, -18, 18);
        playerPosition.z = Mathf.Clamp(playerPosition.z, -14, 18);
        player.position = playerPosition;
        if (move.sqrMagnitude > .01f) player.rotation = Quaternion.LookRotation(move);
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
        player = MakePrimitive(PrimitiveType.Capsule, "Wayfarer", playerPosition, new Vector3(.72f, 1.12f, .72f), new Color(.45f, .75f, .85f), worldRoot).transform;
        MakePrimitive(PrimitiveType.Sphere, "Wayfarer aura", playerPosition + Vector3.up * 1.1f, new Vector3(.2f,.2f,.2f), new Color(.7f,.4f,1), player);
        // Three distant encounter landmarks
        MakeLandmark("Gloamling", new Vector3(-8, .8f, 10), new Color(.63f,.32f,.82f));
        MakeLandmark("Moss Warden", new Vector3(8, 1.1f, 15), new Color(.3f,.65f,.46f));
        MakeLandmark("Hollow Stag", new Vector3(0, 1, 23), new Color(.8f,.6f,.32f));
        player.position = playerPosition;
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
        Text(new Rect(w/2-140,h-50,280,26),"W A S D     MOVE THROUGH THE FOREST",mutedStyle);
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
        float ay=cardY+98, bw=177, gap=14;
        ActionButton(new Rect(px,ay,bw,70),"EXTRACT",$"10 sec  ·  gather Essence",()=>Extract(1f,10f),new Color(.33f,.19f,.47f));
        ActionButton(new Rect(px+bw+gap,ay,bw,70),"FOCUS",$"5 sec  ·  next extraction +60%",Focus,new Color(.13f,.29f,.34f));
        ActionButton(new Rect(px+(bw+gap)*2,ay,bw,70),"DEFEND",$"3 sec  ·  resist behavior",Defend,new Color(.22f,.26f,.38f));
        ActionButton(new Rect(px+(bw+gap)*3,ay,bw,70),"BURST",$"15 sec  ·  extract ×2.4",()=>Extract(2.4f,15f),new Color(.48f,.2f,.28f));
        Box(new Rect(px,ay+82,pw,48),new Color(.035f,.04f,.07f,.91f));
        Text(new Rect(px+14,ay+90,pw-28,32),"FIELD LOG   /   "+combatLog,mutedStyle);
        Button(new Rect(w-190,h-48,165,30),"RETREAT",()=>Finish(false),new Color(.24f,.18f,.2f));
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
        Button(new Rect(x+100,y+292,370,50),"RETURN TO THE VEILWOOD",()=>{screen=GameScreen.Explore;SaveGame();},new Color(.34f,.2f,.48f));
    }
    private void Button(Rect r,string text,Action act,Color color)
    { Box(r,color); if(GUI.Button(r,text,buttonStyle)) act(); }

    private void StartEncounter(int index)
    {
        string[] names={"Gloamling","Moss Warden","Hollow Stag"}, titles={"Veil Wisp","Ancient Sentinel","Eclipse Boss"}, behaviors={"Disrupt · weakens extraction", "Recovery · resistance rises", "Phase Shift · changing forms"};
        float[] max={420,760,1250}, resist={.08f,.18f,.24f};
        enemy=new Enemy { Name=names[index],Title=titles[index],Max=max[index],Essence=max[index],Resistance=resist[index],Behavior=behaviors[index] };
        collected=0; timerMax=75; timer=timerMax; actionMultiplier=1; disruption=0; combatLog="Encounter opened. Choose an action; the timer pauses while you decide.";
        encounters++; screen=GameScreen.Combat;
        if(player != null) { var p=player.position; p.x=0; p.z=0; player.position=p; }
    }
    private void Extract(float mult,float cost)
    {
        if(screen!=GameScreen.Combat || timer<=0) return;
        if(timer<cost) { resultTitle="Extraction window closed"; resultBody="There was not enough time to complete that action. Your gathered Essence remains yours."; Finish(false); return; }
        float output=92f*mult*actionMultiplier*(1-enemy.Resistance)*(1-disruption);
        float gain=Distribute(Mathf.Min(output,enemy.Essence));
        enemy.Essence-=gain; collected+=gain; timer-=cost; actionMultiplier=1;
        combatLog=$"Essence drawn: {gain:0}. {enemy.Name} has {enemy.Essence:0} Essence remaining.";
        disruption=0;
        if(enemy.Essence<=.01f) { enemy.Essence=0; resultTitle="Enemy depleted"; resultBody=$"{enemy.Name} dissolves into a stream of violet light."; Finish(true); }
        else if(TotalEssence()>=TotalCapacity()-.01f) { resultTitle="Capacity reached"; resultBody="Every reservoir is filled. The party secures the encounter."; Finish(true); }
        else EnemyTurn();
        SaveGame();
    }
    private void Focus()
    {
        if(!Spend(5))return; actionMultiplier=1.6f; combatLog="You attune to the flow. Your next extraction is empowered."; EnemyTurn();
    }
    private void Defend()
    {
        if(!Spend(3))return; timer=Mathf.Min(timer+5,timerMax); combatLog="A ward steadies your reservoirs and restores 5 seconds."; EnemyTurn(false);
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
