using Jypeli;
using Jypeli.Assets;
using Jypeli.Controls;
using Jypeli.Widgets;
using System;
using System.Collections.Generic;

///@author Katri Viiliäinen
///@version 12.2.2021
///
/// 
/// <summary>
/// Tasohyppelypeli, jossa pelaaja ohjaa pingviinihahmoa nuolinäppäimillä. 
/// Pelaajan tavoitteena on kerätä mahdollisimman monta kalaa ennen maaliin pääsyään sekä
/// väisteltävä lumileopardeja ja vettä.
/// </summary>
public class PingviiniPeli : PhysicsGame
{
    private const double NOPEUS = 200;
    private const double HYPPYNOPEUS = 750;
    private const int RUUDUN_KOKO = 40;

    private PlatformCharacter pelaaja1;
    private PhysicsObject merileopardi;

    private Image pelaajanKavely = LoadImage("pingviinikavely.png");
    private Image pelaajanKuva = LoadImage("pingviini.png");
    private Image pelaajaHyppy = LoadImage("pingviinihyppy.png");
    private Image kalaKuva = LoadImage("kala.png");              //TODO: muokkaa kuvaa
    private Image merileopardiKuva = LoadImage("merileopardi.png");
    private Image taustakuva = LoadImage("tausta.png");          //TODO: muokkaa kuvaa


    private SoundEffect kalaAani = LoadSoundEffect("maali.wav"); //TODO: muuta äänitehoste
    //TODO: private SoundEffect merileopardiAani = LoadSoundEffect("merileopardi.wav")
    //TODO: private SoundEffect maaliAani = LoadSoundEffect("maali.wav")



    /// <summary>
    /// Luodaan kenttä, määritellään painovoima sekä kameran taso. 
    /// Lisäksi kutsutaan pelaajan ohjaimia aliohjelmasta.
    /// </summary>
    public override void Begin()
    {
        Gravity = new Vector(0, -1000);

        LuoKentta();
        LisaaNappaimet();

        Camera.Follow(pelaaja1);
        Camera.ZoomFactor = 1.2;
        Camera.StayInLevel = true;
    }


    /// <summary>
    /// Aliohjelmassa luodaan kenttä käyttäen erillistä tekstitiedostoa.
    /// Ks. Content kansiosta. # lisää tason, V lisää vettä, * lisää kalan, P lisää pelaajan 
    /// ja M lisää merileopardin. 
    /// </summary>
    public void LuoKentta()
    {
        TileMap kentta = TileMap.FromLevelAsset("kentta1.txt"); //TODO: muuta kenttää
        kentta.SetTileMethod('#', LisaaTaso);
        kentta.SetTileMethod('V', LisaaVesi);
        kentta.SetTileMethod('*', LisaaKala);
        kentta.SetTileMethod('P', LisaaPelaaja);
        kentta.SetTileMethod('M', LisaaMerileopardi);
        kentta.Execute(RUUDUN_KOKO, RUUDUN_KOKO);
        Level.CreateBorders();
        //TODO: POISTA Level.Background.CreateGradient(Color.White, Color.SkyBlue);
        Level.Background.Image = taustakuva;
        Level.Background.FitToLevel();
    }


    /// <summary>
    /// Aliohjelmassa kentän tasojen luomiseksi.
    /// </summary>
    /// <param name="paikka">Paikka, johon taso luodaan</param>
    /// <param name="leveys">Tason leveys</param>
    /// <param name="korkeus"> Tason korkeus</param>
    public void LisaaTaso(Vector paikka, double leveys, double korkeus)
    {
        PhysicsObject taso = PhysicsObject.CreateStaticObject(leveys, korkeus);
        taso.Position = paikka;
        taso.Color = Color.White;
        //TODO: Lisää tekstuuri
        Add(taso);
    }


    /// <summary>
    /// Aliohjelmassa veden luomiseksi.
    /// </summary>
    /// <param name="paikka">Paikka, johon vesipalikat luodaan</param>
    /// <param name="leveys">Vesipalikan leveys</param>
    /// <param name="korkeus">Vesipalikan korkeus</param>
    public void LisaaVesi(Vector paikka, double leveys, double korkeus)
    {
        PhysicsObject vesi = PhysicsObject.CreateStaticObject(leveys, korkeus);
        vesi.Position = paikka;
        vesi.Color = Color.Blue;
        vesi.IgnoresCollisionResponse = true;
        vesi.Tag = "vesi";
        //TODO:Lisää tekstuuri
        Add(vesi);
    }


    //TODO: LisaaMaali
    //TODO: LisaaLaskuri INTMETER, mallia Pong-pelistä


    /// <summary>
    /// Aliohjelma kalojen luomiseksi.
    /// </summary>
    /// <param name="paikka">Paikka, johon kalat luodaan</param>
    /// <param name="leveys">Kalan leveys</param>
    /// <param name="korkeus">Kalan korkeus</param>
    public void LisaaKala(Vector paikka, double leveys, double korkeus)
    {
        PhysicsObject kala = PhysicsObject.CreateStaticObject(leveys, korkeus);
        kala.IgnoresCollisionResponse = true;
        kala.Position = paikka;
        kala.Image = kalaKuva;
        kala.Tag = "kala";
        Add(kala);
    }


    /// <summary>
    /// Aliohjelma merileopardien luomiseksi.
    /// </summary>
    /// <param name="paikka">Paikka, johon merileopardit luodaan</param>
    /// <param name="leveys">Merileopardin leveys</param>
    /// <param name="korkeus">Merileopardin korkeus</param>
    public void LisaaMerileopardi(Vector paikka, double leveys, double korkeus)
    {
        merileopardi = new PhysicsObject (40.0, 40.0);
        merileopardi.Position = paikka;
        merileopardi.Restitution = 0.0;
        merileopardi.Image = merileopardiKuva;
        merileopardi.Tag = "merileopardi";
        MerileopardiLiiku();
        merileopardi.Mass = 1.0;


        Add(merileopardi);

        //TODO: lisää liikkeet
        // erillinen aliohjelmansa? liikkuu edestakaisin tiettyä väliä 
        // esim paikka -100 kääntyy takaisin ja liikkuu kunnes paikka + 100?
        





    }


    public void MerileopardiLiiku()
    {


        Vector liike = new Vector (1000,0)
        merileopardi.Hit(liike);
        // vaihtoehtoisesti merileopardi.Move(liike); tai .MoveTo
        //merileopardi.StopVertical();
        //merileopardi.StopAngular();


    }


    /// <summary>
    /// Aliohjelmassa määritellään pelaajan hahmon ominaisuuksia: massa, animaatiot ja 
    /// mitä pelaajalle tapahtuu muihin olioihin törmätessä.
    /// </summary>
    /// <param name="paikka">Paikka, johon pelaaja luodaan</param>
    /// <param name="leveys">Pelaajan hahmon leveys</param>
    /// <param name="korkeus">Pelaajan hahmon korkeus</param>
    public void LisaaPelaaja(Vector paikka, double leveys, double korkeus)
    {
        pelaaja1 = new PlatformCharacter(leveys, korkeus);
        pelaaja1.Position = paikka;
        pelaaja1.Mass = 4.0;
        pelaaja1.AnimWalk = new Animation(pelaajanKavely);      
        pelaaja1.AnimIdle = new Animation(pelaajanKuva);
        pelaaja1.AnimJump = new Animation(pelaajaHyppy);
        //TODO: pelaaja1.AnimFall = new Animation();


        AddCollisionHandler(pelaaja1, "kala", TormaaKalaan);
        AddCollisionHandler(pelaaja1, "merileopardi", TormaaMerileopardiin);   //TODO: Pystyykö vettä ja merileopardeja yhdistämään samaan törmäykseen?
        AddCollisionHandler(pelaaja1, "vesi", TormaaVeteen);
        Add(pelaaja1);
    }


    /// <summary>
    /// Määritellään pelaajan liike.
    /// </summary>
    /// <param name="hahmo">Pelaajan hahmo</param>
    /// <param name="nopeus">Hahmon nopeus liikuttaessa</param>
    public void Liikuta(PlatformCharacter hahmo, double nopeus)
    {
        hahmo.Walk(nopeus);
        
        
    }


    /// <summary>
    /// Määritellään pelaajan hyppy.
    /// </summary>
    /// <param name="hahmo">Pelaajan hahmo</param>
    /// <param name="nopeus">Hahmon nopeus hypättäessä</param>
    public void Hyppaa(PlatformCharacter hahmo, double nopeus)
    {
        hahmo.Jump(nopeus);
     
    }


    /// <summary>
    /// Kun pelaaja törmää kalaan kala tuhoutuu ja kuuluu ääni.
    /// </summary>
    /// <param name="hahmo">pelaajan hahmo</param>
    /// <param name="kala">pelissä kerättävät esineet</param>
    public void TormaaKalaan(PhysicsObject hahmo, PhysicsObject kala)
    {
        kalaAani.Play();
        MessageDisplay.Add("Keräsit kalan!"); //TODO: POISTA?
        kala.Destroy();
    }


    /// <summary>
    /// Kun pelaajaa törmää merileopardiin pelaajan hahmo tuhoutuu ja kuuluu ääni.
    /// </summary>
    /// <param name="hahmo"></param>
    /// <param name="merileopardi"></param>
    public void TormaaMerileopardiin(PhysicsObject hahmo, PhysicsObject merileopardi)
    {
        //TODO: lisää äänitehosteet
        MessageDisplay.Add("Voi ei, jouduit merileopardin kitaan"); //TODO: Poista? Muuta valikoksi?
        pelaaja1.Destroy();
        //TODO: pelille loppupiste
    }


    /// <summary>
    /// Kun pelaajaa törmää veteen pelaajan hahmo tuhoutuu ja kuuluu ääni.
    /// </summary>
    /// <param name="hahmo">Pelaajan hahmo</param>
    /// <param name="vesi">Vesi</param>
    public void TormaaVeteen (PhysicsObject hahmo, PhysicsObject vesi)
    {
        //TODO: lisää äänitehosteet
        MessageDisplay.Add("Voi ei, putosit veteen ja jouduit merileopardin kitaan"); //TODO: Poista? Muuta valikoksi?
        pelaaja1.Destroy();
        //TODO: pelille loppupiste
    }



    /// <summary>
    /// Pelaajan käyttämien näppäinten määrittely
    /// </summary>
    public void LisaaNappaimet()
    {
        Keyboard.Listen(Key.F1, ButtonState.Pressed, ShowControlHelp, "Näytä ohjeet");
        Keyboard.Listen(Key.Escape, ButtonState.Pressed, ConfirmExit, "Lopeta peli");

        Keyboard.Listen(Key.Left, ButtonState.Down, Liikuta, "Liikkuu vasemmalle", pelaaja1, -NOPEUS);
        Keyboard.Listen(Key.Right, ButtonState.Down, Liikuta, "Liikkuu vasemmalle", pelaaja1, NOPEUS);
        Keyboard.Listen(Key.Up, ButtonState.Pressed, Hyppaa, "Pelaaja hyppää", pelaaja1, HYPPYNOPEUS);

        ControllerOne.Listen(Button.Back, ButtonState.Pressed, Exit, "Poistu pelistä"); 

        ControllerOne.Listen(Button.DPadLeft, ButtonState.Down, Liikuta, "Pelaaja liikkuu vasemmalle", pelaaja1, -NOPEUS);
        ControllerOne.Listen(Button.DPadRight, ButtonState.Down, Liikuta, "Pelaaja liikkuu oikealle", pelaaja1, NOPEUS);
        ControllerOne.Listen(Button.A, ButtonState.Pressed, Hyppaa, "Pelaaja hyppää", pelaaja1, HYPPYNOPEUS);

        PhoneBackButton.Listen(ConfirmExit, "Lopeta peli");
    }


    //TODO: KÄYTTÖLIITTYMÄ kun peli loppuu (maali, vesi, merileopradi) valikko
    //tulokset sekä pelin lopetus ja mahdollisuus yrittää uudelleen (+ seuraava taso?)
}

