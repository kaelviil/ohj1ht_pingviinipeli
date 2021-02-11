using Jypeli;
using Jypeli.Assets;
using Jypeli.Controls;
using Jypeli.Widgets;
using System;
using System.Collections.Generic;

public class PingviiniPeli : PhysicsGame
{
    private const double NOPEUS = 200;
    private const double HYPPYNOPEUS = 750;
    private const int RUUDUN_KOKO = 40;

    private PlatformCharacter pelaaja1;

    private Image pelaajanKuva = LoadImage("pingviini.png"); //TODO: vaihda kuva oikeaan
    private Image kalaKuva = LoadImage("kala.png"); //TODO: vaihda kuva oikeaan
    private Image merileopardiKuva = LoadImage("merileopardi.png");  //TODO: vaihda kuva oikeaan
    private Image taustakuva = LoadImage("tausta.png"); //TODO: vaihda oikeaan kuvaan


    private SoundEffect kalaAani = LoadSoundEffect("maali.wav"); //TODO: vaihda äänitehoste ja lisää äänitehosteet merileopardille, veteen ja maaliin


    public override void Begin()
    {
        Gravity = new Vector(0, -1000);

        LuoKentta();
        LisaaNappaimet();

        Camera.Follow(pelaaja1);
        Camera.ZoomFactor = 1.2;
        Camera.StayInLevel = true;
    }


    public void LuoKentta()
    {
        TileMap kentta = TileMap.FromLevelAsset("kentta1.txt"); //TODO: muuta kenttää
        kentta.SetTileMethod('#', LisaaTaso);
        kentta.SetTileMethod('*', LisaaKala);
        kentta.SetTileMethod('P', LisaaPelaaja);
        kentta.SetTileMethod('M', LisaaMerileopardi);
        kentta.Execute(RUUDUN_KOKO, RUUDUN_KOKO);
        Level.CreateBorders();
        // Level.Background.CreateGradient(Color.White, Color.SkyBlue); //TODO: vaihda oikeaan kuvaan
        Level.Background.Image = taustakuva;
    }


    public void LisaaTaso(Vector paikka, double leveys, double korkeus)
    {
        PhysicsObject taso = PhysicsObject.CreateStaticObject(leveys, korkeus);
        taso.Position = paikka;
        taso.Color = Color.White;
        Add(taso);
    }


    //TODO: LisaaVesi

    //TODO: LisaaMaali

    public void LisaaKala(Vector paikka, double leveys, double korkeus)
    {
        PhysicsObject kala = PhysicsObject.CreateStaticObject(leveys, korkeus);
        kala.IgnoresCollisionResponse = true;
        kala.Position = paikka;
        kala.Image = kalaKuva;
        kala.Tag = "kala";
        Add(kala);
    }


    public void LisaaMerileopardi (Vector paikka, double leveys, double korkeus)
    {
        PhysicsObject merileopardi = new PhysicsObject (40.0, 40.0);
        
        merileopardi.IgnoresCollisionResponse = false;
        merileopardi.Image = merileopardiKuva;
        merileopardi.Tag = "merileopardi";
        // TODO: merileopardi.Hit();
        Add(merileopardi);

        //TODO: lisää liikkeet silmukalla
        // erillinen aliohjelmansa? liikkuu edestakaisin tiettyä väliä tasolla
    }



    //TODO: LisaaLaskuri INTMETER, mallia Pong-pelistä


    public void LisaaPelaaja(Vector paikka, double leveys, double korkeus)
    {
        pelaaja1 = new PlatformCharacter(leveys, korkeus);
        pelaaja1.Position = paikka;
        pelaaja1.Mass = 4.0;
        pelaaja1.Image = pelaajanKuva;
        AddCollisionHandler(pelaaja1, "kala", TormaaKalaan);
        AddCollisionHandler(pelaaja1, "merileopardi", TormaaMerileopardiin);
        //TODO: Collision veteen
        Add(pelaaja1);
    }



    public void Liikuta(PlatformCharacter hahmo, double nopeus)
    {
        hahmo.Walk(nopeus);
        // hahmo.AnimWalk = ;
        //TODO: lisää animaatiot
    }


    public void Hyppaa(PlatformCharacter hahmo, double nopeus)
    {
        hahmo.Jump(nopeus);
        //TODO: lisää animaatiot
    }


    public void TormaaKalaan(PhysicsObject hahmo, PhysicsObject kala)
    {
        kalaAani.Play();
        MessageDisplay.Add("Keräsit kalan!");
        kala.Destroy();
    }

    public void TormaaMerileopardiin(PhysicsObject hahmo, PhysicsObject merileopardi)
    {
        //TODO: lisää äänitehosteet
        MessageDisplay.Add("Voi ei, jouduit merileopardin kitaan");
        pelaaja1.Destroy();
        //TODO: pelille loppupiste
    }

    //TODO: public void TormaaVeteen ()
   // {
           //peli loppuu: samoin tavoin kuin edellä
   // }



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


    //TODO: kun peli loppuu (maali, vesi, merileopradi) valikko
    //tulokset sekä pelin lopetus ja mahdollisuus yrittää uudelleen (+ seuraava taso?)
}

