/*
   Copyright 2015 Esa Leppänen

   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.
*/

using System;
using System.Collections.Generic;
using System.IO;

namespace KoodauspahkinaKesa2015
{
    //
    //
    // Algoritmi tekee muhkeuksista bittimaskin ja tekemällä kahden maskin TAI-operaatio, niin saadaan niiden muhkeus helposti
    // Algoritmi lisäksi vähentää tutkittavia sanapareja seuraavasti:
    // * käsitellään sana vain kerran
    // * käsitellään saman muhkeuden sanat vain kerran: esim. "ne" ja "en"
    // * kun tiedetään maksimimuhkeus, niin säädetään sananpituutta / muhkeuden pituutta sen tiedon perusteella
    // * sanat järjestellään muhkeuden perusteella samaan tietorakenteeseen
    // Puutteet:
    // * algoritmi hyväksyy vain ASCII-merkistön ja a-zåäö
    // * jos sana olisi tavuviivallinen ja kahdella rivillä, esim. joukko-
    //   oppi, niin se olisi kaksi erillistä sanaa. Samalla rivillä olevat tavuviivalliset sanat käsitellään
    //   oikein yhtenä sanana, esim. helluntai-maanantaina

    class Program
    {

        // jokaiselle sananpituudelle 1-72 oma hashi: rivinpituuden maksimi 
        private static Dictionary<string, int>[] sanat = new Dictionary<string, int>[72];

        static void Main(string[] args)
        {
            if(args.Length < 1)
            {
                Console.WriteLine("muhkeus.exe <tiedostonnimi>");
                System.Environment.Exit(-1);
            }
            string tiedostonimi = args[0];

            string[] kirja = null;
            try
            {
                kirja = File.ReadAllLines(tiedostonimi);
            } catch(Exception e)
            {
                Console.Error.WriteLine(e.ToString());
                System.Environment.Exit(-1);
            }

            // sallitut merkit: näiden perusteella tehdään bittimaskin arvot
            string sallitutLower = "-abcdefghijklmnopqrstuvwxyzåäö";
            string sallitutUpper = "-ABCDEFGHIJKLMNOPQRSTUVWXYZÅÄÖ";

            // 256 merkkiä ASCII-taululle: alustetaan nolliksi
            int[] muhkeusSingle = new int[256];
            
            // Tehdään bittimaskit sallituille merkeille
            for(int i=1; i<sallitutLower.Length;i++)
            {
                muhkeusSingle[sallitutLower[i]] = muhkeusSingle[sallitutUpper[i]] = 1 << (i - 1);
            }

            // jokaiselle muhkeusmäärälle 1-30 oma listansa
            var muhkeusTasot = new List<int>[sallitutLower.Length];
            var tulosTasot = new List<int>[sallitutLower.Length];
            
            // alustetaan listat
            for (int i = sallitutLower.Length - 1; i >= 0; i--)
            {
                muhkeusTasot[i] = new List<int>();
                tulosTasot[i] = new List<int>();  
            }

            for (int i = 0; i < 72; i++)
            {
                sanat[i] = new Dictionary<string, int>();
            }

            var kasitellytMuhkeudet = new Dictionary<int, int>();
            
            // Suurin muhkeus
            int maxMuhkeusKoko = 0;

            // käsiteltävän sanan muhkeus
            int curMuhkeusKoko;

            // muhkeus bittimaskina
            int muhkeusMaski;

			// Käydään tiedoston rivit läpi
            for (int j = kirja.Length - 1; j>=0 ; j--)
            {
                // splittaus vie eniten aikaa tässä algoritmissa. Splitataan välimerkkien kohdalta.
                foreach(string buffer in kirja[j].Split(new char[]{' ',',','.',':','"','\'','!','?'}) )
                {
                    int slen = buffer.Length;
                    // Oko sana jo tallennettu?
                    if (!sanat[slen].ContainsKey(buffer))
                    {
                        // Lasketaan sanan muhkeus käsittelemällä kaikki kirjaimet
                        muhkeusMaski = 0;
                        for (int x = slen - 1; x >= 0; x--)
                        {
                            muhkeusMaski |= muhkeusSingle[buffer[x]];
                        }

                        // Lisätään se puskuriin, sanan pituuden kohdalle
                        sanat[slen].Add(buffer, muhkeusMaski);

                        // Onko muhkeusmaski jo puskurissa? "NE" ja "EN" on sama maski!
                        if (!kasitellytMuhkeudet.ContainsKey(muhkeusMaski))
                        {
                            // Lasketaan sanan muhkeus
                            curMuhkeusKoko = NumberOfSetBits(muhkeusMaski);
                                
                            // Lisätään se käsiteltyihin
                            kasitellytMuhkeudet.Add(muhkeusMaski, curMuhkeusKoko);

                            // Lisätään se oikealle tasolle listaan
                            muhkeusTasot[curMuhkeusKoko].Add(muhkeusMaski);

                            // Onko suurin?
                            if (curMuhkeusKoko > maxMuhkeusKoko)
                            {
                                maxMuhkeusKoko = curMuhkeusKoko;
                            }
                        }
                    }
                }
            }

            // Nyt on alustettu, aletaan hakemaan kaikki parit

            // tämä kertoo kuinka suuria pitää sanojen muhkeuksien olla. Eli sanapari1 + sanapari2 pitää olla vähintään näin muhkea
            int maxMuhkeus = maxMuhkeusKoko;

            // Aloitetaan isoimmasta tasosta
            for (int i = maxMuhkeusKoko; i * 2 >= maxMuhkeus; i--)
            {
                // Aloitetaan isoimmasta tasosta myös, niinkauan alaspäin että tasot voivat muodostaa tarpeeksi muhkean parin
                for (int j = i; i + j >= maxMuhkeus; j--)
                {
                    // Haetaan tasojen i ja j kaikki sanat yhteen. Sana voi olla pari itsenä kanssa, mutta 
                    // ne filtteröityy pois, koska sen muhkeus ei muutu
                    for (int x = muhkeusTasot[i].Count - 1; x >= 0; x-- )
                    {
                        for (int y = muhkeusTasot[j].Count - 1; y >= 0; y--)
                        {
                            // Lasketaan sanaparin muhkeus.. Jos tarpeeksi iso, niin lisätään se tason listaan.
                            // Huomaa, että tähän voi mennä sanapari järjestyksessä: A,B ja B,A
                            curMuhkeusKoko = NumberOfSetBits(muhkeusTasot[i][x] | muhkeusTasot[j][y]);
                            if (curMuhkeusKoko >= maxMuhkeus)
                            {
                                maxMuhkeus = curMuhkeusKoko;
                                tulosTasot[curMuhkeusKoko].Add(muhkeusTasot[i][x]);
                                tulosTasot[curMuhkeusKoko].Add(muhkeusTasot[j][y]);
                            }
                        }
                    }
                }
            }

            // Nyt on löydetyt parit listassa. Tulostetaan ne:

            var loydetyt = new Dictionary<long, int>();

            // Sanaparien määrä
            int lask = 1;

            // Sanaparit on pistetty listaan pareittain, siksi kahden hyppy
            for (int i = 0; i < tulosTasot[maxMuhkeus].Count; i += 2)
            {
                // Lasketaan muhkeusmaskista uniikki avain
                long arvo = (((long)tulosTasot[maxMuhkeus][i]) << 32) + (long)tulosTasot[maxMuhkeus][i + 1];

                if (!loydetyt.ContainsKey(arvo))
                {
                    // Samalla muhkeusmaskilla voi olla useampi sana, esim: no, on, oon
                    string[] sanat1 = HaeSana(tulosTasot[maxMuhkeus][i]).Split('|');
                    string[] sanat2 = HaeSana(tulosTasot[maxMuhkeus][i+1]).Split('|');
                    foreach(var sana1 in sanat1)
                    {
                        foreach(var sana2 in sanat2)
                        {
                            Console.WriteLine("Sanapari " + lask++ + ":\t'" + sana1 + "'\t+ '" + sana2 + "'");
                        }
                    }              
                    // molemmat muhkeusmaskit listaan: ei tulosteta useaan kertaan samoja sanapareja
                    loydetyt.Add(arvo, 1);
                    arvo = (((long)tulosTasot[maxMuhkeus][i+1]) << 32) + (long)tulosTasot[maxMuhkeus][i];
                    loydetyt.Add(arvo, 1);
                }
            }

            // Loppu.

        }

        // Hakee muhkeusmaskin perusteella kaikki siihen sopivat sanat
        private static string HaeSana(int muhkeusMaski)
        {
            string paluuSanat = "";

            // Haetaan oikeista sanan pituuksista. Saadaan muhkeuden koko ->
            // tästä tiedetään että sanassa pitää olla vähintään tämän verran kirjaimia.
            int sanaTaso = NumberOfSetBits(muhkeusMaski);

            for(int i=sanaTaso; i<sanat.Length; i++)
            {
                foreach(var key in sanat[i].Keys)
                {
                    if (sanat[i][key] == muhkeusMaski)
                    {
                        if (paluuSanat.Length > 0)
                        {
                            paluuSanat += "|";
                        }
                        paluuSanat += key;
                    }
                }
            }
            return paluuSanat;
        }

        // Laskee bittien määrän 32 bittisestä maskista == sanan muhkeus
        // Population count
        // http://resources.mpi-inf.mpg.de/departments/rg1/teaching/advancedc-ws08/script/lecture10.pdf
        // https://stackoverflow.com/questions/109023/how-to-count-the-number-of-set-bits-in-a-32-bit-integer

        public static int NumberOfSetBits(int i)
        {
            i = i - ((i >> 1) & 0x55555555);
            i = (i & 0x33333333) + ((i >> 2) & 0x33333333);
            return (((i + (i >> 4)) & 0x0F0F0F0F) * 0x01010101) >> 24;
        }
    }
}
