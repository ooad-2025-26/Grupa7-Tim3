# ParkirajBa - Online sistem za rezervaciju parking mjesta 

---

## O projektu

**ParkirajBa** je sveobuhvatan sistem dizajniran za pronalaženje i rezervisanje parking mjetsa. Omogućava:

- Pregled dostupnih parking objekata na interaktivnoj Google Maps karti
- Filtriranje parkinga po lokaciji, cijeni, opremljenosti (EV punjač, kamere, pristup za osobe s invaliditetom)
- Kreiranje i upravljanje rezervacijama s odabirom termina i tipa cijene (sat, dan, mjesec, godina)
- Online plaćanje karticom s Luhn validacijom i slanjem potvrde na email
- QR kod za ulaz i izlaz iz parkinga
- Automatski obračun prekoračenja i dodatnih naknada
- Upravljanje vlastitim parking objektima za vlasnike (dodavanje, editovanje, slike, cjenovnik)
- Admin panel s pregledom korisnika, parkinga, rezervacija i finansijskih izvještaja
- Izvoz izvještaja u PDF i Excel format


Cilj sistema je da poveća efikasnost i poboljša iskustvo pronalaženja parking mjesta.

---

## Online pristup

Aplikacija je hostovana na sljedećem linku:  
🔗 [https://parkirajbaapp-001-site1.ltempurl.com/](https://parkirajbaapp-001-site1.ltempurl.com/)

username: parkirajbaapp-001
password: ParkirajBa2

---

## Testni korisnici

| Uloga    | Email                     | Lozinka       |
|-----------------------|-----------------------------|---------------|
| Admin                 | admin@parkirajba.ba         | Admin123!     |
| Vlasnik parkinga      | dcosovic1@etf.unsa.ba       | Vlasnik123!   |
| Registrovani Korisnik | tbeganovic2@etf.unsa.ba     | Korisnik123!  |

---

## Konekcijski string

```json
"ConnectionStrings": {
  "DefaultConnection": "Data Source=SQL6032.site4now.net;Initial Catalog=db_ac8dcd_parkirajba;User Id=db_ac8dcd_parkirajba_admin;Password=ParkirajBa2"
}
