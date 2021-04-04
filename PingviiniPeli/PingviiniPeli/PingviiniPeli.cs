using Jypeli;
using Jypeli.Assets;
using Jypeli.Controls;
using Jypeli.Widgets;
using System;
using System.Collections.Generic;

///@author Katri Viiliäinen
///@version 12.3.2021
///
/// 
/// <summary>
/// Tasohyppelypeli, jossa pelaaja ohjaa pingviinihahmoa nuolinäppäimillä. 
/// Pelaajan tavoitteena on kerätä mahdollisimman monta kalaa ennen maaliin pääsyään ja
/// väisteltävä lumileopardeja sekä vettä.
/// </summary>

/* Koodi pohjautuu Jypelin valmiiseen tasohyppelypelin pohjaan, 
   Jypelin käyttöohjeisiin (ks. https://trac.cc.jyu.fi/projects/npo/wiki/KirjastonOhjeet viitattu 17.2.2021),
   Pong-pelin koodiin (ks. https://trac.cc.jyu.fi/projects/npo/wiki/Pong/Vaihe1 viitattu 17.2.2021)
   sekä kevään 2021 ohjelmointi 1 kurssilla luentojen aikana tehtyihin koodeihin (https://gitlab.jyu.fi/tie/ohj1/2021k/esimerkit/-/tree/master/ viitattu 17.2.2021). */

// Äänitehosteet ja grafiikka: Katri Viiliäinen

public class PingviiniPeli : PhysicsGame
{
    private const double NOPEUS = 200;
    private const double HYPPYNOPEUS = 750;
    private const int RUUDUN_KOKO = 30;

    private int kenttaNro = 1;
    private IntMeter pelaajanPisteet;
    private List<int> pisteetYhteensa = new List<int>();

    private readonly Image alkuvalikonKuva = LoadImage("alkuvalikonkuva.png");
    private readonly Image taustakuva = LoadImage("tausta.png");
    private readonly Image pelaajanKavely = LoadImage("pingviinikavely.png"); 
    private readonly Image pelaajanKuva = LoadImage("pingviini.png");
    private readonly Image pelaajaHyppy = LoadImage("pingviinihyppy.png");
    private readonly Image pelaajaPutoaa = LoadImage("pingviiniputoaa.png");
    private readonly Image kalaKuva = LoadImage("kala.png");
    private readonly Image maalinKuva = LoadImage("maali.png");
    private readonly Image merileopardiKuva = LoadImage("merileopardi.png");
    private readonly Image vesiMerileopardilla = LoadImage("vesitekstuurimerileopardi.png");      
    private readonly Image vesiKuva = LoadImage("vesitekstuuri.png");
    private readonly Image lumiKuva = LoadImage("lumitekstuuri.png");

    private readonly SoundEffect kalaAani = LoadSoundEffect("kalaaani.wav");
    private readonly SoundEffect merileopardiAani = LoadSoundEffect("merileopardiaani.wav");



    /// <summary>
    /// Luodaan alkuvalikko ja kenttä.
    /// </summary>
    public override void Begin()
    {
        LuoAlkuvalikko();
        Level.Background.Image = alkuvalikonKuva;
        Level.Background.FitToLevel();
       
    }


    /// <summary>
    /// Luo alkuvalikon
    /// </summary>
    //Lähde: https://trac.cc.jyu.fi/projects/npo/wiki/Alkuvalikko (viitattu 26.2.2021)
    private void LuoAlkuvalikko()
    {
        MultiSelectWindow alkuvalikko = new MultiSelectWindow("Pingviinin pako", "Aloita peli", "Lopeta");
        alkuvalikko.AddItemHandler(0, VaihdaKenttaa);
        alkuvalikko.AddItemHandler(1, Exit);
        alkuvalikko.Color = Color.LightBlue;
        Add(alkuvalikko);
    }


    /// <summary>
    /// Lopetusvalikko, kun pelaajan hahmo tuhoutuu törmätessään veteen tai merileopardiin.
    /// </summary>
    private void LopetusvalikkoTuhoutuessa()
    {
        MultiSelectWindow lopetusvalikko = new MultiSelectWindow("Voi ei, jouduit merileopardin kitaan.", "Yritä uudestaan", "Lopeta");
        lopetusvalikko.AddItemHandler(0, AloitaAlusta);
        lopetusvalikko.AddItemHandler(1, Exit);
        lopetusvalikko.Color = Color.LightBlue;
        Add(lopetusvalikko);
    }


    /// <summary>
    /// Lopetusvalikko, kun pelaaja pääsee maaliin.
    /// </summary>
    private void LopetusvalikkoMaali()
    {
        MultiSelectWindow lopetusvalikko = new MultiSelectWindow("Onneksi olkoon! Pääsit turvallisesti kotiin.", "Seuraava taso", "Yritä uudestaan", "Lopeta");
        lopetusvalikko.AddItemHandler(0, VaihdaKenttaa);
        lopetusvalikko.AddItemHandler(1, AloitaAlusta);
        lopetusvalikko.AddItemHandler(2, Exit);
        lopetusvalikko.Color = Color.LightBlue;
        Add(lopetusvalikko);
    }


    /// <summary>
    /// Pelin kenttä vaihtuu sen perusteella, mikä kentän numero on sen vaihtuessa.
    /// Määritellään samalla kentän painovoima sekä kamera.
    /// Pelin saa laitettua pauselle.
    /// </summary>
    //Lähde: https://trac.cc.jyu.fi/projects/npo/wiki/KenttienLiittaminen (viitattu 17.3.2021) ja https://trac.cc.jyu.fi/projects/npo/wiki/Pause (viitattu 4.4.2021)
    private void VaihdaKenttaa()
    {
        ClearAll();

        if (kenttaNro == 1) LuoKentta("kentta1.txt");
        else if (kenttaNro == 2) LuoKentta("kentta2.txt");
        else if (kenttaNro == 3) LuoKentta("kentta3.txt");
        else if (kenttaNro > 3) Exit();

        Gravity = new Vector(0, -1200);

        Camera.ZoomFactor = 1.2;
        Camera.StayInLevel = true;

        LisaaLaskuri();
       

        IsPaused = true;
        Pause();
    }


    /// <summary>
    /// Mahdollistaa kentän aloittamisen alusta pelaajan tuhoutuessa tai päästessä maaliin.
    /// </summary>
    // Lähde: https://trac.cc.jyu.fi/projects/npo/wiki/KentanTekeminen (viitattu 28.2.2021)
    private void AloitaAlusta()
    {
        if (kenttaNro > 1)
        {
            kenttaNro--;
        }

        ClearAll();
        LisaaLaskuri();
        VaihdaKenttaa();
    }


    /// <summary>
    /// Kenttä luodaan käyttäen erillistä tekstitiedostoa.
    /// Ks. Content kansiosta kentta1, kentta2 ja kentta3.
    /// # lisää tason, V ja v lisää vettä eri tekstuureilla, * lisää kalan, P lisää pelaajan, 
    /// M lisää merileopardin ja L maalin. 
    /// </summary>
    // Lähde: Jypelin tasohyppelypelin pohja. 
    private void LuoKentta(string tasonTiedostonimi)
    {
        TileMap kentta = TileMap.FromLevelAsset(tasonTiedostonimi);
        kentta.SetTileMethod('#', LisaaTaso);
        kentta.SetTileMethod('V', LisaaVesi, "vesitekstuurimerileopardi.png");
        kentta.SetTileMethod('v', LisaaVesi, "vesitekstuuri.png");
        kentta.SetTileMethod('*', LisaaObjekti, "kala", "kala.png");
        kentta.SetTileMethod('P', LisaaPelaaja);
        kentta.SetTileMethod('M', LisaaMerileopardi);
        kentta.SetTileMethod('m', LisaaMerileopardi);
        kentta.SetTileMethod('L', LisaaObjekti, "maali", "maali.png");
        kentta.Execute(RUUDUN_KOKO, RUUDUN_KOKO);
        Level.CreateBorders();
        Level.Background.Image = taustakuva;
        Level.Background.FitToLevel();
    }


    /// <summary>
    /// Aliohjelma tasojen luomiseksi.
    /// </summary>
    /// <param name="paikka">Paikka, johon taso luodaan</param>
    /// <param name="leveys">Tason leveys</param>
    /// <param name="korkeus"> Tason korkeus</param>
    //Lähde: Jypelin tasohyppelypelin pohja.
    private void LisaaTaso(Vector paikka, double leveys, double korkeus)
    {
        PhysicsObject taso = PhysicsObject.CreateStaticObject(leveys, korkeus);
        taso.Position = paikka;
        taso.Image = lumiKuva;
        taso.Tag = "taso";
        Add(taso);
    }


    /// <summary>
    /// Aliohjelma veden luomiseksi.
    /// </summary>
    /// <param name="paikka">Paikka, johon vesipalikka luodaan</param>
    /// <param name="leveys">Vesipalikan leveys</param>
    /// <param name="korkeus">Vesipalikan korkeus</param>
    /// <param name="kuvanNimi">Vesipalikan kuvantiedoston nimi</param>
    private void LisaaVesi(Vector paikka, double leveys, double korkeus, string kuvanNimi)
    {
        PhysicsObject vesi = PhysicsObject.CreateStaticObject(leveys, korkeus);
        vesi.Position = paikka;
        vesi.Image = LoadImage(kuvanNimi);
        vesi.IgnoresCollisionResponse = true;
        vesi.Tag = "vesi";
        Add(vesi);
    }


    /// <summary>
    /// Luo staattisen objektin: kala tai maali. 
    /// </summary>
    /// <param name="paikka">Paikka, johon objekti luodaan</param>
    /// <param name="leveys">Objektin leveys</param>
    /// <param name="korkeus">Objektin korkeus</param>
    /// <param name="tagi">Objektin tägi</param>
    /// <param name="kuvanNimi">Objektin kuvatiedoston nimi</param>
    private void LisaaObjekti(Vector paikka, double leveys, double korkeus, string tagi, string kuvanNimi)
    {
        PhysicsObject objekti = PhysicsObject.CreateStaticObject(leveys * 1.25, korkeus * 1.25);
        objekti.Position = paikka;
        objekti.Image = LoadImage(kuvanNimi);
        objekti.IgnoresCollisionResponse = true;
        objekti.Tag = tagi;
        Add(objekti);
    }


    /// <summary>
    /// Luo merileopardin
    /// </summary>
    /// <param name="paikka">Paikka, johon merileopardit luodaan</param>
    /// <param name="leveys">Merileopardin leveys</param>
    /// <param name="korkeus">Merileopardin korkeus</param>
    private void LisaaMerileopardi(Vector paikka, double leveys, double korkeus)
    {
        PlatformCharacter merileopardi = new PlatformCharacter(1.25 * leveys, 1.25 * korkeus);
        merileopardi.Position = paikka;
        merileopardi.Mass = 10.0;
        merileopardi.Shape = Shape.Triangle;                            //Kolmion muotoinen, jotta törmäys pelaajna kanssa toimii tarkemmin.
        merileopardi.Image = merileopardiKuva;
        merileopardi.Tag = "merileopardi";
        MerileopardinLiike(merileopardi);
        Add(merileopardi);
    }


    /// <summary>
    /// Aivot, joiden avulla merileopardi liikkuu tasolla edestakaisin.
    /// </summary>
    /// <param name="hahmo">Merileopardi</param>
    // Lähde: https://trac.cc.jyu.fi/projects/npo/wiki/Aivot (viitattu 28.2.2021)
    private void MerileopardinLiike (PlatformCharacter hahmo)
    {
        PlatformWandererBrain liike = new PlatformWandererBrain();
        liike.Speed = 50;
        hahmo.Brain = liike;
    }


    /// <summary>
    /// Määritellään pelaajan hahmon ominaisuuksia: massa sekä animaatiot. 
    /// Kutsutaan näppäimmet määrittävää aliohjelmaa sekä
    /// törmäylsenkäsittelijöitä.
    /// </summary>
    /// <param name="paikka">Paikka, johon pelaaja luodaan</param>
    /// <param name="leveys">Pelaajan hahmon leveys</param>
    /// <param name="korkeus">Pelaajan hahmon korkeus</param>
    //Lähde: Jypelin tasohyppelypelin pohja.
    private void LisaaPelaaja(Vector paikka, double leveys, double korkeus)
    {
        PlatformCharacter pelaaja = new PlatformCharacter(leveys, korkeus);
        pelaaja.Position = paikka;
        pelaaja.Mass = 4.0;
        pelaaja.AnimWalk = new Animation(pelaajanKavely);
        pelaaja.AnimIdle = new Animation(pelaajanKuva);
        pelaaja.AnimJump = new Animation(pelaajaHyppy);
        pelaaja.AnimFall = new Animation(pelaajaPutoaa);
        pelaaja.Tag = "pelaaja";

        LuoOhjaimet(pelaaja);
        KasittelePelaajanTormays(pelaaja);

        Camera.Follow(pelaaja);

        Add(pelaaja);
    }


    /// <summary>
    /// Luodaan ohjaimet, joiden avulla pelaaja pystyy ohjaamaan pelihahmoaan, lopettamaan pelin, 
    /// laittamaan peli pauselle sekä katsomaan ohjeet.
    /// </summary>
    /// <param name="pelaaja">Pelaajan hahmo</param>
    //Lähde: Jypelin tasohyppelipelin pohja.
    private void LuoOhjaimet (PlatformCharacter pelaaja)
    {
        Keyboard.Listen(Key.F1, ButtonState.Pressed, ShowControlHelp, "Näytä ohjeet");                                             
        Keyboard.Listen(Key.Q, ButtonState.Pressed, Pause, "Pysäyttää pelin. Paina uudelleen jatkaaksesi peliä");       //Lähde: https://trac.cc.jyu.fi/projects/npo/wiki/Pause (viitattu 4.4.2021)
        Keyboard.Listen(Key.Escape, ButtonState.Pressed, ConfirmExit, "Lopeta peli");
        Keyboard.Listen(Key.Left, ButtonState.Down, LiikutaPelaajaa, "Liikkuu vasemmalle", pelaaja, -NOPEUS);
        Keyboard.Listen(Key.Right, ButtonState.Down, LiikutaPelaajaa, "Liikkuu oikealle", pelaaja, NOPEUS);
        Keyboard.Listen(Key.Up, ButtonState.Pressed, PelaajanHyppy, "Hyppää", pelaaja, HYPPYNOPEUS);
        PhoneBackButton.Listen(ConfirmExit, "Lopeta peli");
    }


    /// <summary>
    /// Määritellään pelaajan liike.
    /// </summary>
    /// <param name="pelaaja">Pelaajan hahmo</param>
    /// <param name="nopeus">Hahmon nopeus liikuttaessa</param> 
    // Lähde: Jypelin tasohyppelypelin pohja.
    private void LiikutaPelaajaa(PlatformCharacter pelaaja, double nopeus)
    {
        pelaaja.Walk(nopeus);
    }


    /// <summary>
    /// Määritellään pelaajan hyppy.
    /// </summary>
    /// <param name="pelaaja">Pelaajan hahmo</param>
    /// <param name="nopeus">Hahmon nopeus hypättäessä</param> 
    //Lähde: Jypelin tasohyppelypelin pohja.
    private void PelaajanHyppy(PlatformCharacter pelaaja, double nopeus)
    {
        pelaaja.Jump(nopeus);
    }


    /// <summary>
    /// Törmäyksenkäsittelijät, kun pelaajan hahmo törmää kerättäviin esinsiin (kalaan), merileopardiin, veteen tai maaliin.
    /// </summary>
    /// <param name="pelaaja">Pelaajan hamo</param>
    //Lähde: https://trac.cc.jyu.fi/projects/npo/wiki/Pong/Vaihe7 (viitattu 17.2.2021).
    private void KasittelePelaajanTormays (PlatformCharacter pelaaja)
    {
        AddCollisionHandler(pelaaja, "kala", TormaaKerattavaan);
        AddCollisionHandler(pelaaja, "merileopardi", TormaaVeteenTaiLeopardiin);
        AddCollisionHandler(pelaaja, "vesi", TormaaVeteenTaiLeopardiin);
        AddCollisionHandler(pelaaja, "maali", TormaaMaaliin);
    }


    /// <summary>
    /// Pelaajaan törmätessä veteen ta merileopardiin pelaajan hahmo tuhoutuu ja kuuluu ääni.
    /// </summary>
    /// <param name="pelaaja">pelaajan hahmo</param>
    /// <param name="kohde">kohde, johon pelaaja törmää</param>
    private void TormaaVeteenTaiLeopardiin(PhysicsObject pelaaja, PhysicsObject kohde)
    {
        kenttaNro++;
        merileopardiAani.Play();
        pelaaja.Destroy();
        LopetusvalikkoTuhoutuessa();
    }


    /// <summary>
    /// Kun pelaaja törmää kerätävään objektiin (esim. kalaan) se tuhoutuu ja kuuluu ääni.
    /// </summary>
    /// <param name="pelaaja">pelaajan hahmo</param>
    /// <param name="kohde">pelissä kerättävät esineet</param>
    //Lahde: https://trac.cc.jyu.fi/projects/npo/wiki/Pong/Vaihe7 (viitattu 17.2.2021) & Jypelin tasohyppelypelin pohja.
    private void TormaaKerattavaan(PhysicsObject pelaaja, PhysicsObject kohde)
    {
        kalaAani.Play();
        pelaajanPisteet.Value += 1;
        kohde.Destroy();
    }


    /// <summary>
    /// Pelaajan törmätessä maaliin peli loppuu.
    /// </summary>
    /// <param name="hahmo">pelaajan hahmo</param>
    /// <param name="kohde">merileopardi</param>
    private void TormaaMaaliin(PhysicsObject hahmo, PhysicsObject kohde)
    {
        PisteetKentista(pelaajanPisteet);
        kenttaNro++;
        LopetusvalikkoMaali();
    }


    /// <summary>
    /// Pelaajan pistelaskurin sijainnin määrittely.
    /// </summary>
    //Lähde: https://trac.cc.jyu.fi/projects/npo/wiki/Pong/Vaihe7 (viitattu 17.2.2021). Laskurin sijaintia muokattu.
    private IntMeter LisaaLaskuri()
    {
        pelaajanPisteet = LuoPistelaskuri(Screen.Right - 50.0, Screen.Top - 50.0);
        return pelaajanPisteet;
    }


    /// <summary>
    /// Ruutu, joka näyttää pelaajan keräämät pisteet.
    /// </summary>
    /// <param name="x">Laskurinäytön keskipisteen X koordinaatti</param>
    /// <param name="y">Laskurinäytön keskipisteen y koordinaatti</param>
    /// <returns>pelaajan pisteet</returns>
    //Lähde: https://trac.cc.jyu.fi/projects/npo/wiki/Pong/Vaihe7 (viitattu 17.2.2020)
    private IntMeter LuoPistelaskuri(double x, double y)
    {
        pelaajanPisteet = new IntMeter(0);

        Label naytto = new Label();
        naytto.BindTo(pelaajanPisteet);
        naytto.X = x;
        naytto.Y = y;
        naytto.TextColor = Color.Black;
        Add(naytto);

        return pelaajanPisteet;
    }


    /// <summary>
    /// Pelaajan kaikista tasoista keräämät pisteet. Tulos tulostetaan ruudulle viimeisen kentän päätyttyä. 
    /// </summary>
    private void PisteetKentista(IntMeter keratytPisteet)
    {
        pisteetYhteensa.Add(keratytPisteet.Value);
        int summa = SummaaPisteet(pisteetYhteensa);

        if (kenttaNro == 3)
        {
            Label pisteRuutu = new Label(430.0, 50.0, "Keräsit yhteensä " + summa.ToString() + " kalaa");
            pisteRuutu.X = Screen.Left + 512;
            pisteRuutu.Y = Screen.Top - 240;
            pisteRuutu.Color = Color.LightBlue;
            pisteRuutu.TextColor = Color.Black;
            Add(pisteRuutu);
        }

        //TODO: POISTA, testaa funktion toimintaa tason vaihtuessa; MessageDisplay.Add("Pisteitä " + summa.ToString());

        //TODO: pisteiden tulostus niin, että joka kentän lopussa näkyy kentästä kerätyst pisteet SEKÄ pisteiden yhteismäärä.
    }


    /// <summary>
    /// Lasketaan yhteen pelaaja keräämät pisteet. Jos lista on tyhjä palautetaan 0.
    /// </summary>
    /// <param name="pisteet">Lista pelaajan keräämistä pisteistä</param>
    /// <returns>Pisteiden summa</returns>
    private static int SummaaPisteet(List<int> pisteet)
    {
        if (pisteet.Count == 0)
        {
            return 0;
        }

        int summa = 0;
        for (int i = 0; i < pisteet.Count; i++)
        {
            summa = summa + pisteet[i];
        }

        return summa;
    }
}