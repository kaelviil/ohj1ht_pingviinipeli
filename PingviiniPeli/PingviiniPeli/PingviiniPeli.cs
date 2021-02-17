using Jypeli;
using Jypeli.Assets;
using Jypeli.Controls;
using Jypeli.Widgets;
using System;
using System.Collections.Generic;

///@author Katri Viiliäinen
///@version 17.2.2021
///
/// 
/// <summary>
/// Tasohyppelypeli, jossa pelaaja ohjaa pingviinihahmoa nuolinäppäimillä. 
/// Pelaajan tavoitteena on kerätä mahdollisimman monta kalaa ennen maaliin pääsyään sekä
/// väisteltävä lumileopardeja ja vettä.
/// </summary>

// Koodi pohjautuu Jypelin valmiiseen tasohyppelypelipohjaan, 
// kevään 2021 ohjelmointi 1 kurssilla luentojen aikana tehtyihin koodeihin (https://gitlab.jyu.fi/tie/ohj1/2021k/esimerkit/-/tree/master/ viitattu 17.2.2021),
// Pong-pelin koodiin (ks. https://trac.cc.jyu.fi/projects/npo/wiki/Pong/Vaihe1 viitattu 17.2.2021)
// sekä Jypelin käyttöohjeisiin (ks. https://trac.cc.jyu.fi/projects/npo/wiki/KirjastonOhjeet viitattu 17.2.2021).

//TODO: tarkenna kurssin aikana tehtyjä koodeja oikeisiin kansioihinsa (Erityisesti pelottava peli -> tekoäly vihollisen liikkeisiin)
public class PingviiniPeli : PhysicsGame
{
    private const double NOPEUS = 200;
    private const double HYPPYNOPEUS = 750;
    private const int RUUDUN_KOKO = 30;

    private PlatformCharacter pelaaja;             //TODO: kannattaako muutta omaksi luokakseen?

    private PlatformCharacter merileopardi;       //TODO: kannattaako muutta omaksi luokakseen? ja onko paempi olla Physics/GameObject
    private PhysicsObject kala;
    private PhysicsObject vesi;
    private PhysicsObject maali;

    private IntMeter pelaajanPisteet;

    //TODO: vaihtoehtoinen kävely private Image[] pelaajanKavely = LoadImages("pingviinikavely.png", "pingviinikavely2", "pingviinikavely.png");
    private Image pelaajanKavely = LoadImage("pingviinikavely.png");
    private Image pelaajanKuva = LoadImage("pingviini.png");
    private Image pelaajaHyppy = LoadImage("pingviinihyppy.png");
    private Image pelaajaPutoaa = LoadImage("pingviiniputoaa.png");
    private Image kalaKuva = LoadImage("kala.png");                 //TODO: muokkaa kuvaa
    private Image merileopardiKuva = LoadImage("merileopardi.png");
    
    private Image taustakuva = LoadImage("tausta.png");         
    private Image tasonKuva = LoadImage("lumitekstuuri.png");       //TODO: muokaa paremmin erottuvammaksi vedestä+taustasta    
    private Image vedenKuva = LoadImage("vesitekstuuri2.png");       //TODO: muokaa veden pintaa Gimpissä


    private SoundEffect kalaAani = LoadSoundEffect("maali.wav");    //TODO: muuta äänitehoste tai sitten sama kuin maaliin pääsyssä
    //TODO: private SoundEffect merileopardiAani = LoadSoundEffect("merileopardi.wav")
    //TODO: private SoundEffect maaliAani = LoadSoundEffect("maali.wav")



    /// <summary>
    /// Luodaan kenttä, määritellään painovoima sekä kameran taso. 
    /// Lisäksi kutsutaan pelaajan ohjaimia aliohjelmasta.
    /// </summary>
    
    //Lähde: Jypelin tasohyppelypelin pohjaan
    public override void Begin()
    {
        Gravity = new Vector(0, -1200);

        LuoKentta();
        LisaaNappaimet();

        Camera.Follow(pelaaja);
        Camera.ZoomFactor = 1.2;
        Camera.StayInLevel = true;
    }


    /// <summary>
    /// Kenttä luodaan käyttäen erillistä tekstitiedostoa.
    /// Ks. Content kansiosta kentta1.txt
    /// # lisää tason, V lisää vettä, * lisää kalan, P lisää pelaajan, 
    /// M lisää merileopardin ja § maalin. 
    /// </summary>
    
    // Lähde: Jypelin tasohyppelypelin pohja. Koodia muokattu. 

    public void LuoKentta()
    {
        TileMap kentta = TileMap.FromLevelAsset("kentta1.txt"); //TODO: muuta kenttää
        kentta.SetTileMethod('#', LisaaTaso);
        kentta.SetTileMethod('V', LisaaVesi);
        kentta.SetTileMethod('*', LisaaKala);
        kentta.SetTileMethod('P', LisaaPelaaja);
        kentta.SetTileMethod('M', LisaaMerileopardi);
        kentta.SetTileMethod('§', LisaaMaali);
        kentta.Execute(RUUDUN_KOKO, RUUDUN_KOKO);
        Level.CreateBorders();
        //TODO: POISTA Level.Background.CreateGradient(Color.White, Color.SkyBlue);
        Level.Background.Image = taustakuva;
        Level.Background.FitToLevel();
        LisaaLaskuri();
    }


    /// <summary>
    /// Aliohjelma kentän tasojen luomiseksi.
    /// </summary>
    /// <param name="paikka">Paikka, johon taso luodaan</param>
    /// <param name="leveys">Tason leveys</param>
    /// <param name="korkeus"> Tason korkeus</param>

    //Lähde: Jypelin tasohyppelypelin pohja. Koodia muokattu.

    public void LisaaTaso(Vector paikka, double leveys, double korkeus)
    {
        PhysicsObject taso = PhysicsObject.CreateStaticObject(leveys, korkeus);
        taso.Position = paikka;
        taso.Image = tasonKuva;
        Add(taso);
    }


    /// <summary>
    /// Aliohjelma veden luomiseksi.
    /// </summary>
    /// <param name="paikka">Paikka, johon vesipalikat luodaan</param>
    /// <param name="leveys">Vesipalikan leveys</param>
    /// <param name="korkeus">Vesipalikan korkeus</param>
    public void LisaaVesi(Vector paikka, double leveys, double korkeus)
    {
        vesi = PhysicsObject.CreateStaticObject(leveys, korkeus);
        vesi.Position = paikka;
        vesi.Image = vedenKuva;
        vesi.IgnoresCollisionResponse = true;
        vesi.Tag = "vesi";
        Add(vesi);
    }

    /// <summary>
    /// Luodaan peliin maali.
    /// </summary>
    /// <param name="paikka">Paikka, johon maali luodaan</param>
    /// <param name="leveys">Maalin leveys</param>
    /// <param name="korkeus">Maalin korkeus</param>
    public void LisaaMaali(Vector paikka, double leveys, double korkeus)
    {
        maali = PhysicsObject.CreateStaticObject(leveys, korkeus);
        maali.Position = paikka;
        maali.Color = Color.Red; //TODO: muuta kuvaksi
        maali.IgnoresCollisionResponse = true;
        maali.Tag = "maali";
        Add(maali);
    }


    /// <summary>
    /// Pelaajan pistelaskurin sijainnin määrittely.
    /// </summary>

    //Lähde: https://trac.cc.jyu.fi/projects/npo/wiki/Pong/Vaihe7 (viitattu 17.2.2021). Laskurin sijaintia muokattu.
    public void LisaaLaskuri()
    {
        pelaajanPisteet = LuoPistelaskuri(Screen.Right - 50.0, Screen.Top - 50.0);          //TODO: kokeile onko parempi vasemmassa reunassa + kuva pisteiden viereen?
    }


    /// <summary>
    /// Aliohjelma kalojen luomiseksi.
    /// </summary>
    /// <param name="paikka">Paikka, johon kalat luodaan</param>
    /// <param name="leveys">Kalan leveys</param>
    /// <param name="korkeus">Kalan korkeus</param>
   
    //Lähde: Jypelin tasohyppelypelin pohja. Koodia muokattu.

    public void LisaaKala(Vector paikka, double leveys, double korkeus)
    {
        kala = PhysicsObject.CreateStaticObject(leveys, korkeus);
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
        merileopardi = new PlatformCharacter (leveys, korkeus);
        merileopardi.Position = paikka;
        merileopardi.Restitution = 0.0;
        merileopardi.Image = merileopardiKuva;
        merileopardi.Tag = "merileopardi";
        merileopardi.Mass = 10.0;
        Add(merileopardi);

        //TODO: lisää liikkeet
        // erillinen aliohjelmansa? liikkuu edestakaisin tiettyä väliä 
        // esim paikka -100 kääntyy takaisin ja liikkuu kunnes paikka + 100?
        
    }

    //TODO: 
    public void MerileopardiLiiku(PlatformCharacter hahmo, double nopeus)
    {

        for (int i = 0; i < RUUDUN_KOKO - 60; i++)
        {
            hahmo.Walk(NOPEUS);
        }
        
        
        // mitä eroa Hit, Push, Move, MoveTo ja Walk?
      
    }


    /// <summary>
    /// Aliohjelmassa määritellään pelaajan hahmon ominaisuuksia: massa, animaatiot ja 
    /// mitä pelaajalle tapahtuu muihin olioihin törmätessä.
    /// </summary>
    /// <param name="paikka">Paikka, johon pelaaja luodaan</param>
    /// <param name="leveys">Pelaajan hahmon leveys</param>
    /// <param name="korkeus">Pelaajan hahmon korkeus</param>
    ///     
    ///Lähde: Jypelin tasohyppelypelin pohja. Koodia muokattu.
    public void LisaaPelaaja(Vector paikka, double leveys, double korkeus)
    {
        pelaaja = new PlatformCharacter(leveys, korkeus);
        pelaaja.Position = paikka;
        pelaaja.Mass = 4.0;
        pelaaja.AnimWalk = new Animation(pelaajanKavely);      
        pelaaja.AnimIdle = new Animation(pelaajanKuva);
        pelaaja.AnimJump = new Animation(pelaajaHyppy);
        pelaaja.AnimFall = new Animation(pelaajaPutoaa);


        AddCollisionHandler(pelaaja, "kala", TormaaKalaan);
        AddCollisionHandler(pelaaja, "merileopardi", KasittelePelaajanTormays);   
        AddCollisionHandler(pelaaja, "vesi", KasittelePelaajanTormays);
        AddCollisionHandler(pelaaja, "maali", KasittelePelaajanTormays);
        Add(pelaaja);
    }


    /// <summary>
    /// Määritellään pelaajan liike.
    /// </summary>
    /// <param name="hahmo">Pelaajan hahmo</param>
    /// <param name="nopeus">Hahmon nopeus liikuttaessa</param>
         
    // Lähde: Jypelin tasohyppelypelin pohja.

    public void Liikuta(PlatformCharacter hahmo, double nopeus)
    {
            hahmo.Walk(nopeus);  
    }


    /// <summary>
    /// Määritellään pelaajan hyppy.
    /// </summary>
    /// <param name="hahmo">Pelaajan hahmo</param>
    /// <param name="nopeus">Hahmon nopeus hypättäessä</param> 

    //Lähde: Jypelin tasohyppelypelin pohja.

    public void Hyppaa(PlatformCharacter hahmo, double nopeus)
    {
        hahmo.Jump(nopeus);
        
    }


    /// <summary>
    /// Kun pelaajaa törmää maaliin peli/taso loppuu ja kuuluu ääni.
    /// Kun pelaajaa törmää merileopardiin pelaajan hahmo tuhoutuu ja kuuluu ääni.
    /// Kun pelaajaa törmää veteen pelaajan hahmo tuhoutuu ja kuuluu ääni.
    /// </summary>
    /// <param name="hahmo">pelaajan hahmo</param>
    /// <param name="kohde">kohde, johon pelaaja törmää</param>

    //Lähde: https://trac.cc.jyu.fi/projects/npo/wiki/Pong/Vaihe7 (viitattu 17.2.2021). Koodia muokattu
    public void KasittelePelaajanTormays(PhysicsObject hahmo, PhysicsObject kohde)
    {

        if (kohde == maali)
        {
            MessageDisplay.Add("Onneksi olkoon! Pääsit turvallisesti kotiin"); //TODO: muuta valikoksi
        }

        else if (kohde == merileopardi)
        {
            MessageDisplay.Add("Voi ei, jouduit merileopardin kitaan"); //TODO: Muuta valikoksi
            pelaaja.Destroy();
        }

        else if (kohde == vesi)
        {
            MessageDisplay.Add("Voi ei, törmäsit veteen ja jouduit merileopardin kitaan"); //TODO: Muuta valikoksi
            pelaaja.Destroy();
        }

        else if ( kohde == kala)
        {
            TormaaKalaan(hahmo, kala);  //TODO: miten saa järkevämmin lisättyä KasittelePelaajanTormays aliohjelmaan, niin että kaikki kalat lasketaan
        }

        //TODO: Törmäys animaatio veteen

        //TODO: lisää äänitehosteet


        //TODO: pelille loppupiste ja sopivat valikot
    }


    /// <summary>
    /// Kun pelaaja törmää kalaan kala tuhoutuu ja kuuluu ääni.
    /// </summary>
    /// <param name="hahmo">pelaajan hahmo</param>
    /// <param name="kala">pelissä kerättävät esineet</param>

    //Lahde: https://trac.cc.jyu.fi/projects/npo/wiki/Pong/Vaihe7 (viitattu 17.2.2021) & Jypelin Tasohyppelypelin pohja.
    public void TormaaKalaan(PhysicsObject hahmo, PhysicsObject kala)
    {
        kalaAani.Play();
        pelaajanPisteet.Value += 1;
        kala.Destroy();
    }


    /// <summary>
    /// Laskuri pelaajan kaloista keräämien pisteiden laskemiseen
    /// </summary>
    /// <param name="x">Laskurinäytön keskipisteen X koordinaatti</param>
    /// <param name="y">Laskurinäytön keskipisteen y koordinaatti</param>
    /// <returns></returns>

    //Lähde: https://trac.cc.jyu.fi/projects/npo/wiki/Pong/Vaihe7 (viitattu 17.2.2020)
    public IntMeter LuoPistelaskuri(double x, double y)
    {
        IntMeter laskuri = new IntMeter(0);
  
        Label naytto = new Label();
        naytto.BindTo(laskuri);
        naytto.X = x;
        naytto.Y = y;
        naytto.TextColor = Color.Black;
        Add(naytto);

        return laskuri;
    }


    /// <summary>
    /// Pelaajan käyttämien näppäinten määrittely
    /// </summary>
    /// 

    //Lähde: Jypelin tasohyppelypelin pohjaan.

    public void LisaaNappaimet()
    {
        Keyboard.Listen(Key.F1, ButtonState.Pressed, ShowControlHelp, "Näytä ohjeet");
        Keyboard.Listen(Key.Escape, ButtonState.Pressed, ConfirmExit, "Lopeta peli");

        Keyboard.Listen(Key.Left, ButtonState.Down, Liikuta, "Liikkuu vasemmalle", pelaaja, -NOPEUS);
        Keyboard.Listen(Key.Right, ButtonState.Down, Liikuta, "Liikkuu vasemmalle", pelaaja, NOPEUS);
        Keyboard.Listen(Key.Up, ButtonState.Pressed, Hyppaa, "Pelaaja hyppää", pelaaja, HYPPYNOPEUS);


        PhoneBackButton.Listen(ConfirmExit, "Lopeta peli");
    }


    //TODO: KÄYTTÖLIITTYMÄ kun peli loppuu (maali, vesi, merileopradi) valikko
    //tulokset sekä pelin lopetus ja mahdollisuus yrittää uudelleen (+ seuraava taso?)
}

