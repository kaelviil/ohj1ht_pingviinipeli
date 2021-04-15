using Jypeli;
using Jypeli.Assets;
using Jypeli.Controls;
using Jypeli.Widgets;
using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

///@author Katri Viiliäinen
///@version 15.4.2021
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

//TODO: ohjeet tulee oman koneen levyasemalta, pystyykö muuttamaan niin, että ei sidottu yhteen koneeseen?

public class PingviininPako : PhysicsGame
{
    /// <summary>
    /// Liikkuvien olioiden nopeus.
    /// </summary>
    private const double NOPEUS = 200;
    /// <summary>
    /// Liikkuien olioiden nopeus hypätessä.
    /// </summary>
    private const double HYPPYNOPEUS = 750;
    /// <summary>
    /// Ruudun koko.
    /// </summary>
    private const int RUUDUN_KOKO = 30;
    /// <summary>
    /// Lähde ohjesivun tekstiin.
    /// </summary>
    private const string POLKU = @"C:\ohjelmointi\kurssit\ohj1\harkka\PingviiniPeli\PingviiniPeli\Content\ohjeteksti.txt";

    /// <summary>
    /// Kenttä numero. Kenttä vaihtuu luvun perusteella.
    /// </summary>
    private int kenttaNro = 1;
    /// <summary>
    /// Pistemittari, joka kerää tietoa pelaajan kentästä keräämistä pisteistä.
    /// </summary>
    private IntMeter pelaajanPisteet;
    /// <summary>
    /// Lista, johon tallennetaan pelaajan kentistä keräämät pisteet.
    /// </summary>
    private List<int> pisteetYhteensa = new List<int>();
    /// <summary>
    /// 10 parhaan pelaajan pisteet.
    /// </summary>
    private EasyHighScore parhaatPisteet = new EasyHighScore();

    /// <summary>
    /// Kuvatiedostot.
    /// </summary>
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

    /// <summary>
    /// Äänitehosteet.
    /// </summary>
    private readonly SoundEffect kalaAani = LoadSoundEffect("kalaaani.wav");
    private readonly SoundEffect merileopardiAani = LoadSoundEffect("merileopardiaani.wav");


    /// <summary>
    /// Luodaan alkuvalikko pelin aloittamiseksi.
    /// </summary>
    public override void Begin()
    {
        LuoAlkuvalikko();
        Level.Background.Image = alkuvalikonKuva;
        Level.BackgroundColor = Color.White;
        Level.Background.FitToLevel();
    }


    /// <summary>
    /// Luo alkuvalikon.
    /// </summary>
    //Lähde: https://trac.cc.jyu.fi/projects/npo/wiki/Alkuvalikko (viitattu 26.2.2021)
    private void LuoAlkuvalikko()
    {
        MultiSelectWindow alkuvalikko = new MultiSelectWindow("Pingviinin pako", "Aloita peli", "Ohjeet", "Parhaat pisteet", "Lopeta");
        alkuvalikko.AddItemHandler(0, VaihdaKenttaa);
        alkuvalikko.AddItemHandler(1, Ohjeet);
        alkuvalikko.AddItemHandler(2, Top10);
        alkuvalikko.AddItemHandler(3, Exit);
        alkuvalikko.Color = Color.LightBlue;
        Add(alkuvalikko);
    }


    /// <summary>
    /// Luo alkuvalikon, pelin loputtua.
    /// </summary>
    //Lähde: https://trac.cc.jyu.fi/projects/npo/wiki/Alkuvalikko (viitattu 26.2.2021) ja https://trac.cc.jyu.fi/projects/npo/wiki/HighScore (viitattu 9.4.2021)
    private void PalaaAlkuvalikkoon(Window sender)
    {
        MultiSelectWindow alkuvalikko = new MultiSelectWindow("Pingviinin pako", "Aloita peli", "Parhaat pisteet", "Ohjeet", "Lopeta");
        alkuvalikko.AddItemHandler(0, VaihdaKenttaa);
        alkuvalikko.AddItemHandler(1, Top10);
        alkuvalikko.AddItemHandler(2, Ohjeet);
        alkuvalikko.AddItemHandler(3, Exit);
        alkuvalikko.Color = Color.LightBlue;
        Add(alkuvalikko);

        if (kenttaNro == 3)
        {
            IsPaused = true;
            PalaaEnsimmaiseenKenttaan();
        }
    }


    /// <summary>
    /// Lopetusvalikko, kun pelaajan hahmo tuhoutuu törmätessään veteen tai merileopardiin.
    /// </summary>
    private void LopetusvalikkoTuhoutuessa()
    {
        MultiSelectWindow lopetusvalikko = new MultiSelectWindow("Voi ei, jouduit merileopardin kitaan.", "Yritä uudestaan", "Lopeta");
        lopetusvalikko.AddItemHandler(0, YritaUudestaan);
        lopetusvalikko.AddItemHandler(1, Exit);
        lopetusvalikko.Color = Color.LightBlue;
        Add(lopetusvalikko);
    }


    /// <summary>
    /// Lopetusvalikko, kun pelaaja pääsee maaliin.
    /// </summary>
    private void LopetusvalikkoMaali()
    { 
            MultiSelectWindow lopetusvalikko = new MultiSelectWindow("Onneksi olkoon! Pääsit turvallisesti maaliin.", "Seuraava taso", "Lopeta");
            lopetusvalikko.AddItemHandler(0, VaihdaKenttaa);
            lopetusvalikko.AddItemHandler(1, Exit);
            lopetusvalikko.Color = Color.LightBlue;
            Add(lopetusvalikko);
            Pisteet(pelaajanPisteet, lopetusvalikko.X, lopetusvalikko.Top);
    }


    /// <summary>
    /// Pelin kenttä vaihtuu sen perusteella, mikä kentän numero on sen vaihtuessa.
    /// Määritellään samalla kentän painovoima sekä kamera, pause ominaisuus ja lisätään laskuri, ClearAll() komennon vuoksi.
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
    /// Tyhjentää pelaajan yhteensä keräämät pisteet sisältävän taulukon sekä mahdollistaa ensimmäisen kentän aloittamisen pelin loputtua jo kerran. 
    /// </summary>
    private void PalaaEnsimmaiseenKenttaan()
    {
        kenttaNro = 1;

        int i = 0;
        while (i < pisteetYhteensa.Count)
        {
            if (pisteetYhteensa[i] >= int.MinValue) pisteetYhteensa.RemoveAt(i);
            else i++;
        }
    }


    /// <summary>
    /// Valikko, joka näyttää parhaan 10 pelaajan pisteet.
    /// </summary>
    private void Top10()
    {
        parhaatPisteet.Show();
        parhaatPisteet.HighScoreWindow.Closed += PalaaAlkuvalikkoon;
    }


    /// <summary>
    /// Ohjesivu.
    /// </summary>
    private void Ohjeet()
    { 
        MultiSelectWindow valikko = new MultiSelectWindow("Ohjeet", "Takaisin");
        valikko.AddItemHandler(0, LuoAlkuvalikko);
        valikko.Closed += PalaaAlkuvalikkoon;

        Label ohjeet = new Label(500.0, 400.0);
        ohjeet.Color = Color.LightBlue;
        ohjeet.Y = valikko.Top + 250;
        ohjeet.TextColor = Color.Black;
        ohjeet.Text = LueTeksti();
        valikko.Add(ohjeet);

        Add(valikko);
    }


    /// <summary>
    /// Luetaan tiedot ulkoisesta tietolähteestä ja muunnetaan ne yhdeksi String-olioksi, niin, että jokaisien rivin väliin tulee rivinvaihto.
    /// </summary>
    /// <returns>Palauttaa ulkoisesta lähteestä luetun tekstin.</returns>
    private String LueTeksti()
    {
        string[] luetutRivit = File.ReadAllLines(POLKU);
        StringBuilder temp = new StringBuilder();
        foreach (string merkkijono in luetutRivit)
        {
            temp.Append(merkkijono + '\n');
        }

        string teksti = temp.ToString();  
        return teksti;
    }


    /// <summary>
    /// Mahdollistaa kentän aloittamisen alusta pelaajan tuhoutuessa.
    /// </summary>
    // Lähde: https://trac.cc.jyu.fi/projects/npo/wiki/KentanTekeminen (viitattu 28.2.2021)
    private void YritaUudestaan()
    {
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
        kentta.SetTileMethod('V', LisaaObjekti, "vesi", "vesitekstuurimerileopardi.png");
        kentta.SetTileMethod('v', LisaaObjekti, "vesi", "vesitekstuuri.png");
        kentta.SetTileMethod('*', LisaaObjekti, "kala", "kala.png", 1.25);
        kentta.SetTileMethod('P', LisaaPelaaja);
        kentta.SetTileMethod('M', LisaaMerileopardi);
        kentta.SetTileMethod('m', LisaaMerileopardi);
        kentta.SetTileMethod('L', LisaaObjekti, "maali", "maali.png", 1.25);
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
    /// Luo staattisen objektin, jonka kokoa voi muuttaa kertoimen avulla.
    /// </summary>
    /// <param name="paikka">Paikka, johon objekti luodaan</param>
    /// <param name="leveys">Objektin leveys</param>
    /// <param name="korkeus">Objektin korkeus</param>
    /// <param name="tagi">Objektin tägi</param>
    /// <param name="kuvanNimi">Objektin kuvatiedoston nimi</param>
    /// <param name="kerroin">Kerroin, jonka avulla objektin kokoa voi muuttaa</param>
    private void LisaaObjekti(Vector paikka, double leveys, double korkeus, string tagi, string kuvanNimi, double kerroin)
    {
        PhysicsObject objekti = PhysicsObject.CreateStaticObject(leveys * kerroin, korkeus * kerroin);
        objekti.Position = paikka;
        objekti.Image = LoadImage(kuvanNimi);
        objekti.IgnoresCollisionResponse = true;
        objekti.Tag = tagi;
        Add(objekti);
    }


    /// <summary>
    /// Luo staattisen objektin. 
    /// </summary>
    /// <param name="paikka">Paikka, johon objekti luodaan</param>
    /// <param name="leveys">Objektin leveys</param>
    /// <param name="korkeus">Objektin korkeus</param>
    /// <param name="tagi">Objektin tägi</param>
    /// <param name="kuvanNimi">Objektin kuvatiedoston nimi</param>
    private void LisaaObjekti(Vector paikka, double leveys, double korkeus, string tagi, string kuvanNimi)
    {
        PhysicsObject objekti = PhysicsObject.CreateStaticObject(leveys, korkeus);
        objekti.Position = paikka;
        objekti.Image = LoadImage(kuvanNimi);
        objekti.IgnoresCollisionResponse = true;
        objekti.Tag = tagi;
        Add(objekti);
    }


    /// <summary>
    /// Luo merileopardin.
    /// </summary>
    /// <param name="paikka">Paikka, johon merileopardit luodaan</param>
    /// <param name="leveys">Olion leveys</param>
    /// <param name="korkeus">olion korkeus</param>
    private void LisaaMerileopardi(Vector paikka, double leveys, double korkeus)
    {
        PlatformCharacter merileopardi = new PlatformCharacter(leveys * 1.25, korkeus * 1.25);
        merileopardi.Position = paikka;
        merileopardi.Mass = 10.0;
        merileopardi.Shape = Shape.Triangle;                            
        merileopardi.Image = merileopardiKuva;
        merileopardi.Tag = "merileopardi";
        MerileopardinLiike(merileopardi);
        Add(merileopardi);
    }


    /// <summary>
    /// Aivot, joiden avulla merileopardi liikkuu edestakaisin pinnoilla.
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
    /// Määritellään pelaajan hahmon ominaisuuksia: muoto, massa sekä animaatiot. 
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
        Keyboard.Listen(Key.Left, ButtonState.Down, LiikutaPelaajaa, "Liikkuu vasemmalle", pelaaja, -NOPEUS);
        Keyboard.Listen(Key.Right, ButtonState.Down, LiikutaPelaajaa, "Liikkuu oikealle", pelaaja, NOPEUS);
        Keyboard.Listen(Key.Up, ButtonState.Pressed, PelaajanHyppy, "Hyppää", pelaaja, HYPPYNOPEUS);
        Keyboard.Listen(Key.Q, ButtonState.Pressed, Pause, "Pysäyttää pelin. Paina uudelleen jatkaaksesi peliä");       //Lähde: https://trac.cc.jyu.fi/projects/npo/wiki/Pause (viitattu 4.4.2021)
        Keyboard.Listen(Key.Escape, ButtonState.Pressed, ConfirmExit, "Lopeta peli");
        Keyboard.Listen(Key.F1, ButtonState.Pressed, ShowControlHelp, "Näytä ohjeet");
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
    /// Törmäyksenkäsittelijät, kun pelaajan hahmo törmää kerättävään esinseen (kalaan), merileopardiin, veteen tai maaliin.
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
    /// Pelaajan törmätessä maaliin kokoanispistemäärään lisätään kentästä kerätyt pisteet sekä luodaan lopetusvalikot tilanteesta riippuen.
    /// </summary>
    /// <param name="hahmo">pelaajan hahmo</param>
    /// <param name="kohde">merileopardi</param>
    private void TormaaMaaliin(PhysicsObject hahmo, PhysicsObject kohde)
    {
        pisteetYhteensa.Add(pelaajanPisteet.Value);

        if (kenttaNro == 3)
        {
            parhaatPisteet.EnterAndShow(SummaaPisteet(pisteetYhteensa));
            parhaatPisteet.HighScoreWindow.Closed += PalaaAlkuvalikkoon;              //Lähde: https://trac.cc.jyu.fi/projects/npo/wiki/HighScore (viitattu 9.4.2021)
        }
        else
        {
            kenttaNro++;
            LopetusvalikkoMaali();
        }
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
    /// Näyttää pelaajan kentästä keräämät pisteet sekä tämän koko pelin aikana keräämät pisteet. 
    /// </summary>
    /// <param name="keratytPisteet">Pelaajan keräämät pisteet</param>
    /// <param name="x">X koordinaatin sijainti</param>
    /// <param name="y">Y koordinaatin sijainti</param>
    private void Pisteet(IntMeter keratytPisteet, double x, double y)
    {
        int summa = SummaaPisteet(this.pisteetYhteensa);

        Label pisteetYhteensa = new Label(430.0, 50.0, "Olet kerännyt yhteensä " + summa.ToString() + " kalaa");
        pisteetYhteensa.X = x;
        pisteetYhteensa.Y = y + 35;
        pisteetYhteensa.Color = Color.LightBlue;
        pisteetYhteensa.TextColor = Color.Black;
        Add(pisteetYhteensa);

        Label pisteetKentasta = new Label(430.0, 50.0, "Keräsit kentästä " + pelaajanPisteet.Value + " kalaa");
        pisteetKentasta.X = x;
        pisteetKentasta.Y = pisteetYhteensa.Top + 10;
        pisteetKentasta.Color = Color.LightBlue;
        pisteetKentasta.TextColor = Color.Black;
        Add(pisteetKentasta);
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