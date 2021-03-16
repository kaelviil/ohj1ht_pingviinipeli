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
/// Pelaajan tavoitteena on kerätä mahdollisimman monta kalaa ennen maaliin pääsyään sekä
/// väisteltävä lumileopardeja ja vettä.
/// </summary>

// Koodi pohjautuu Jypelin valmiiseen tasohyppelypelipohjaan, 
// kevään 2021 ohjelmointi 1 kurssilla luentojen aikana tehtyihin koodeihin (https://gitlab.jyu.fi/tie/ohj1/2021k/esimerkit/-/tree/master/ viitattu 17.2.2021),
// Pong-pelin koodiin (ks. https://trac.cc.jyu.fi/projects/npo/wiki/Pong/Vaihe1 viitattu 17.2.2021)
// sekä Jypelin käyttöohjeisiin (ks. https://trac.cc.jyu.fi/projects/npo/wiki/KirjastonOhjeet viitattu 17.2.2021).

//TODO: tarkenna kurssin aikana tehtyjä koodeja oikeisiin kansioihinsa
public class PingviiniPeli : PhysicsGame
{   
    private const double NOPEUS = 200;
    private const double HYPPYNOPEUS = 750;
    private const int RUUDUN_KOKO = 30;

    int kenttaNro = 1;              //TODO: muuta toteutusta?

    private readonly Image taustakuva = LoadImage("tausta.png");             
    private readonly Image pelaajanKavely = LoadImage("pingviinikavely.png");           //vaihtoehtoinen kävely private Image[] pelaajanKavely = LoadImages("pingviinikavely.png", "pingviinikavely2", "pingviinikavely.png");
    private readonly Image pelaajanKuva = LoadImage("pingviini.png");
    private readonly Image pelaajaHyppy = LoadImage("pingviinihyppy.png");
    private readonly Image pelaajaPutoaa = LoadImage("pingviiniputoaa.png");
    private readonly Image kalaKuva = LoadImage("kala.png");
    private readonly Image maalinKuva = LoadImage("maali.png");
    private readonly Image merileopardiKuva = LoadImage("merileopardi.png");
    private readonly Image vesiMerileopardilla = LoadImage("vesitekstuuri");            //TODO: tarkista vesitekstuurien koot ja nimeä selkeämmin
    private readonly Image vesiKuva = LoadImage("vesitekstuuri2.png");
    private readonly Image lumiKuva = LoadImage("lumitekstuuri.png");     

    private readonly SoundEffect kalaAani = LoadSoundEffect("maali.wav");    
    private readonly SoundEffect merileopardiAani = LoadSoundEffect("merileopardiaani.wav");
        

    /// <summary>
    /// Luodaan alkuvalikko, kenttä ja kutsutaan pelaajan ohjaimia ja pistelaskuria aliohjelmasta.
    /// </summary>
    //Lähde: Jypelin tasohyppelypelin pohja ja https://trac.cc.jyu.fi/projects/npo/wiki/Pause (viitattu 28.2.2021)
    public override void Begin()
    {
        LuoAlkuvalikko();
        VaihdaTasoa();
        IntMeter pelaajanPisteet = new IntMeter(0);
        LisaaLaskuri(pelaajanPisteet);
    }

    private void VaihdaTasoa()
    {
        ClearAll();

        if (kenttaNro == 1) LuoKentta("kentta1");
        else if (kenttaNro == 2) LuoKentta("kentta2");
        else if (kenttaNro == 3) LuoKentta("kentta3"); ///TODO: lopetusvalikko + tulokset
        else if (kenttaNro > 3) Exit();  //TODO: ---> tulos ruuduksi
        
        IsPaused = true;
        Pause();

    }

    /// <summary>
    /// Kenttä luodaan käyttäen erillistä tekstitiedostoa.
    /// Ks. Content kansiosta kentta1, kentta2 ja kentta3
    /// # lisää tason, V ja v lisää veden eritekstuureilla, * lisää kalan, P lisää pelaajan, 
    /// M lisää merileopardin ja L maalin. 
    /// </summary>
    // Lähde: Jypelin tasohyppelypelin pohja. 
    private void LuoKentta(string tasonTiedosto)
    {
        TileMap kentta = TileMap.FromLevelAsset(tasonTiedosto); //TODO: muuta kenttää ja lisää toinen ja kolmas
        kentta.SetTileMethod('#', LisaaTaso);
        kentta.SetTileMethod('V', LisaaVesi, "vesitekstuuri");
        kentta.SetTileMethod('v', LisaaVesi, "vesitekstuuri2");
        kentta.SetTileMethod('*', LisaaObjekti, "kala", "kala.png");
        kentta.SetTileMethod('P', LisaaPelaaja);
        kentta.SetTileMethod('M', LisaaMerileopardi);                   
        kentta.SetTileMethod('m', LisaaMerileopardi);
        kentta.SetTileMethod('L', LisaaObjekti, "maali", "maali.png");
        kentta.Execute(RUUDUN_KOKO, RUUDUN_KOKO);
        Level.CreateBorders();
        Level.Background.Image = taustakuva;
        Level.Background.FitToLevel();

        Gravity = new Vector(0, -1200);

        //Camera.Follow(pelaaja);  -> LuoPelaaja aliohjelmassa
        Camera.ZoomFactor = 1.2;
        Camera.StayInLevel = true;
    }


    /// <summary>
    /// Aliohjelma kentän tasojen luomiseksi.
    /// </summary>
    /// <param name="paikka">Paikka, johon taso luodaan</param>
    /// <param name="leveys">Tason leveys</param>
    /// <param name="korkeus"> Tason korkeus</param>
    //Lähde: Jypelin tasohyppelypelin pohja. Koodia muokattu.
    private void LisaaTaso(Vector paikka, double leveys, double korkeus)
    {
        PhysicsObject taso = PhysicsObject.CreateStaticObject(leveys, korkeus);
        taso.Position = paikka;
        taso.Image = lumiKuva;
        Add(taso);
    }


    /// <summary>
    /// Aliohjelma veden luomiseksi.
    /// </summary>
    /// <param name="paikka">Paikka, johon vesipalikat luodaan</param>
    /// <param name="leveys">Vesipalikan leveys</param>
    /// <param name="korkeus">Vesipalikan korkeus</param>
    private void LisaaVesi(Vector paikka, double leveys, double korkeus, string kuvanNimi)
    {
        PhysicsObject vesi = PhysicsObject.CreateStaticObject(leveys, korkeus);
        vesi.Position = paikka;
        vesi.Image = LoadImage (kuvanNimi);                                 
        vesi.IgnoresCollisionResponse = true;
        vesi.Tag = "vesi";
        Add(vesi);
    }


    /// <summary>
    /// Luo staattisen objekti. (Kala tai maali)
    /// </summary>
    /// <param name="paikka">Paikka, johon maali luodaan</param>
    /// <param name="leveys">Maalin leveys</param>
    /// <param name="korkeus">Maalin korkeus</param>
    private void LisaaObjekti(Vector paikka, double leveys, double korkeus, string tagi, string kuvanNimi)
    {
        PhysicsObject objekti = PhysicsObject.CreateStaticObject(leveys*1.25, korkeus*1.25);
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
    // Lähde: https://trac.cc.jyu.fi/projects/npo/wiki/Aivot (viitattu 28.2.2021)
    private void LisaaMerileopardi(Vector paikka, double leveys, double korkeus)
    {
        PlatformCharacter merileopardi = new PlatformCharacter (1.25* leveys, 1.25*korkeus);
        merileopardi.Position = paikka;
        merileopardi.Mass = 10.0;
        merileopardi.Image = merileopardiKuva;
        merileopardi.Tag = "merileopardi";

        Add(merileopardi);

        PlatformWandererBrain liike = new PlatformWandererBrain();
        liike.Speed = 50;
        merileopardi.Brain = liike;
    }


    /// <summary>
    /// Määritellään pelaajan hahmon ominaisuuksia: massa, animaatiot, näppäimet sekä
    /// mitä pelaajalle tapahtuu muihin olioihin törmätessä.
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

        Camera.Follow(pelaaja);

        Keyboard.Listen(Key.F1, ButtonState.Pressed, ShowControlHelp, "Näytä ohjeet");                                                              //TODO: LuoOhjaimet aliohjelma erikseen?
        Keyboard.Listen(Key.Q, ButtonState.Pressed, Pause, "Pysäyttää pelin. Paina uudelleen jatkaaksesi peliä");
        Keyboard.Listen(Key.Escape, ButtonState.Pressed, ConfirmExit, "Lopeta peli");
        Keyboard.Listen(Key.Left, ButtonState.Down, Liikuta, "Liikkuu vasemmalle", pelaaja, -NOPEUS);
        Keyboard.Listen(Key.Right, ButtonState.Down, Liikuta, "Liikkuu vasemmalle", pelaaja, NOPEUS);
        Keyboard.Listen(Key.Up, ButtonState.Pressed, Hyppaa, "Pelaaja hyppää", pelaaja, HYPPYNOPEUS);
        PhoneBackButton.Listen(ConfirmExit, "Lopeta peli");

        AddCollisionHandler(pelaaja, "kala", TormaaKalaan);
        AddCollisionHandler(pelaaja, "merileopardi", TormaaVeteenTaiLeopardiin);   
        AddCollisionHandler(pelaaja, "vesi", TormaaVeteenTaiLeopardiin);
        AddCollisionHandler(pelaaja, "maali", TormaaMaaliin);

        Add(pelaaja);
    }


    /// <summary>
    /// Määritellään pelaajan liike.
    /// </summary>
    /// <param name="pelaaja">Pelaajan hahmo</param>
    /// <param name="nopeus">Hahmon nopeus liikuttaessa</param> 
    // Lähde: Jypelin tasohyppelypelin pohja.
    private void Liikuta(PlatformCharacter pelaaja, double nopeus)
    {
            pelaaja.Walk(nopeus);  
    }


    /// <summary>
    /// Määritellään pelaajan hyppy.
    /// </summary>
    /// <param name="pelaaja">Pelaajan hahmo</param>
    /// <param name="nopeus">Hahmon nopeus hypättäessä</param> 
    //Lähde: Jypelin tasohyppelypelin pohja.
    private void Hyppaa(PlatformCharacter pelaaja, double nopeus)
    {
        pelaaja.Jump(nopeus);
    }


    /// <summary>
    /// Pelaajaan törmätessä veteen ta merileopardiin pelaajan hahmo tuhoutuu ja kuuluu ääni.
    /// </summary>
    /// <param name="pelaaja">pelaajan hahmo</param>
    /// <param name="kohde">kohde, johon pelaaja törmää</param>

    //Lähde: https://trac.cc.jyu.fi/projects/npo/wiki/Pong/Vaihe7 (viitattu 17.2.2021). Koodia muokattu
    private void TormaaVeteenTaiLeopardiin(PhysicsObject pelaaja, PhysicsObject kohde)               
    {
        kenttaNro++;
        merileopardiAani.Play();
        pelaaja.Destroy();
        LopetusvalikkoTuhoutuessa();
    }


    /// <summary>
    /// Kun pelaaja törmää kalaan kala tuhoutuu ja kuuluu ääni.
    /// </summary>
    /// <param name="pelaaja">pelaajan hahmo</param>
    /// <param name="kala">pelissä kerättävät esineet</param>
    //Lahde: https://trac.cc.jyu.fi/projects/npo/wiki/Pong/Vaihe7 (viitattu 17.2.2021) & Jypelin Tasohyppelypelin pohja.
    private void TormaaKalaan(PhysicsObject pelaaja, PhysicsObject kala)
    {
        kalaAani.Play();                  
        //pelaajanPisteet.Value += 1;
        kala.Destroy();
    }


    /// <summary>
    /// Pelaajan törmätessä maaliin peli loppuu.
    /// </summary>
    /// <param name="hahmo">pelaajan hahmo</param>
    /// <param name="kohde">merileopardi</param>
    private void TormaaMaaliin(PhysicsObject hahmo, PhysicsObject kohde)
    {
        kalaAani.Play();
        int kentastaSaadutPisteet = 0; // pelaajanPisteet.Value;
        PisteetYhteensa(kentastaSaadutPisteet);
        kenttaNro++;
        LopetusvalikkoMaali();
    }


    /// <summary>
    /// Peli alkaa, kun alkuvalikosta painetaan näppäintä "Aloita peli"
    /// </summary>
    //Lähde: https://trac.cc.jyu.fi/projects/npo/wiki/Alkuvalikko (viitattu 26.2.2021)
    private void AloitaPeli()
    {
        //TODO: pystyykö tekemään niin, että peli ruutu tulee näkyviin vasta kun painaa "Aloita peli"?
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
        VaihdaTasoa();
    }


    /// <summary>
    /// Pelaajan pistelaskurin sijainnin määrittely.
    /// </summary>
    //Lähde: https://trac.cc.jyu.fi/projects/npo/wiki/Pong/Vaihe7 (viitattu 17.2.2021). Laskurin sijaintia muokattu.
    private IntMeter LisaaLaskuri(IntMeter laskuri)
    {
        laskuri = LuoPistelaskuri(Screen.Right - 50.0, Screen.Top - 50.0, laskuri);          //TODO: kokeile onko parempi vasemmassa reunassa?
        return laskuri;
    }


    /// <summary>
    /// Ruutu, joka näyttää pelaajan keräämät pisteet.
    /// </summary>
    /// <param name="x">Laskurinäytön keskipisteen X koordinaatti</param>
    /// <param name="y">Laskurinäytön keskipisteen y koordinaatti</param>
    /// <returns>pelaajan pisteet</returns>
    //Lähde: https://trac.cc.jyu.fi/projects/npo/wiki/Pong/Vaihe7 (viitattu 17.2.2020)
    private IntMeter LuoPistelaskuri(double x, double y, IntMeter laskuri)
    {
        Label naytto = new Label();
        naytto.BindTo(laskuri);
        naytto.X = x;
        naytto.Y = y;
        naytto.TextColor = Color.Black;
        Add(naytto);

        return laskuri;
    }


    /// <summary>
    /// Pelaajan kaikista tasoista kerätyt pistemäärät.
    /// </summary>
    private void PisteetYhteensa(int saadutPisteet)
    {
       List <int> pisteetYhteensa = new List<int> ();
       pisteetYhteensa.Add(saadutPisteet);
       SummaaPisteet(pisteetYhteensa);

        //TODO: lisää kentistä kerätyt pisteet listaan [0] = kenttä 1, [1] = kenttä 2, [2] kenttä 3
    }


    /// <summary>
    /// Lasketaan yhteen pelaaja keräämät pisteet.
    /// </summary>
    /// <param name="pisteet">Lista pelaajan keräämistä pisteistä</param>
    /// <returns>Pisteiden summa</returns>
    /// <example>
    /// <pre name="test">
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


    /// <summary>
    /// Luo alkuvalikon
    /// </summary>
    //Lähde: https://trac.cc.jyu.fi/projects/npo/wiki/Alkuvalikko (viitattu 26.2.2021)
    private void LuoAlkuvalikko()
    {
        MultiSelectWindow alkuvalikko = new MultiSelectWindow("Pingviinin pako", "Aloita peli", "Lopeta");
        alkuvalikko.AddItemHandler(0, AloitaPeli);
        alkuvalikko.AddItemHandler(1, Exit);
        alkuvalikko.Color = Color.LightBlue;
        Add(alkuvalikko);

        //TODO: Widgetiksi
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

        //TODO: Widgetiksi
    }


    /// <summary>
    /// Lopetusvalikko, kun pelaaja pääsee maaliin.
    /// </summary>
    private void LopetusvalikkoMaali()
    {
        MultiSelectWindow lopetusvalikko = new MultiSelectWindow("Onneksi olkoon! Pääsit turvallisesti kotiin.", "Seuraava taso", "Yritä uudestaan", "Lopeta");
        lopetusvalikko.AddItemHandler(0, VaihdaTasoa);
        lopetusvalikko.AddItemHandler(1, AloitaAlusta);
        lopetusvalikko.AddItemHandler(2, Exit);
        lopetusvalikko.Color = Color.LightBlue;
        Add(lopetusvalikko);

        //TODO: Widgetiksi / aivan viimeinen lopetusvalikko, joka näyttää pelaajan yhteispisteet mahdollisuus aloittaa peli kokonaan alusta tai lopettaa / mahdollisuus nähdä yhteensä kerätyt pisteet aina, kun vaihtaa tasoa
    }

}