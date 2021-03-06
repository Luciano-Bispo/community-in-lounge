using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using api_comil.Interfaces;
using api_comil.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;


namespace api_comil.Repositorios
{
    public class EventoRepositorio : IEvento
    {
        communityInLoungeContext db = new communityInLoungeContext();


        public async Task<ActionResult<Evento>> Post(Evento evento)
        {
            Categoria categoria = await db.Categoria.Where(c => c.CategoriaId == evento.CategoriaId).FirstOrDefaultAsync();
            Sala sala = await db.Sala.Where(s => s.SalaId == evento.SalaId).FirstOrDefaultAsync();
            Comunidade comunidade = await db.Comunidade.Where(c => c.ComunidadeId == evento.ComunidadeId).FirstOrDefaultAsync();

            if (categoria != null && sala != null && comunidade != null)
            {

                try
                {
                    db.Add(evento);
                    await db.SaveChangesAsync();
                    return evento;
                }
                catch (System.Exception)
                {
                    throw;
                }
            }
            else
            {
                return null;
            }


        }



        public async Task<Evento> Reject(Evento evento, int idResponsavel)
        {
            evento.StatusEvento = "Recusado";
            db.Evento.Update(evento);


            db.ResponsavelEventoTw.Add(new ResponsavelEventoTw { Evento = evento.EventoId, ResponsavelEvento = idResponsavel });

            await db.SaveChangesAsync();
            Mensagem(evento.EmailContato, evento.StatusEvento);
            return evento;
        }



        public async Task<Evento> Accept(Evento evento, int responsavel)
        {
            evento.StatusEvento = "Aprovado";
            db.Evento.Update(evento);

            db.ResponsavelEventoTw.Add(new ResponsavelEventoTw { Evento = evento.EventoId, ResponsavelEvento = responsavel });
            await db.SaveChangesAsync();
            Mensagem(evento.EmailContato, evento.StatusEvento);
            return evento;
        }


        public async Task<Evento> Get(int id)
        {
            var evento = await db.Evento
            .Include(i => i.Categoria)
            .Include(c => c.Sala)
            .Include(c => c.Comunidade)
            .ThenInclude(c => c.ResponsavelUsuario)
            .Include(c => c.Sala)
            .Where(w => w.DeletadoEm == null)
            .FirstOrDefaultAsync(f => f.EventoId == id);

            return evento;
        }

        public async Task<ActionResult<List<Evento>>> Get()
        {
            var listEven = await db.Evento
           .Include(i => i.Categoria)
           .Include(i => i.Comunidade)
           .Include(i => i.Sala)
           .Where(w => w.StatusEvento == "Aprovado")
           .Where(w => w.DeletadoEm == null)
           .ToListAsync();

            foreach (var item in listEven)
            {
                item.Categoria.Evento = null;
                item.Categoria.EventoTw = null;
                item.Comunidade.Evento = null;
                item.Sala.Evento = null;
                item.Sala.EventoTw = null;
            }

            return listEven;
        }


        public async Task<ActionResult<List<Evento>>> PendingMounth(int mes)
        {
            return await db.Evento
                .Include(i => i.Comunidade)
                .Include(i => i.Sala)
                .Include(i => i.Categoria)
                .Where(w => w.DeletadoEm == null)
                .Where(w => w.StatusEvento == "Pendente")
                .Where(w => w.EventoData.Month == mes)
                .OrderBy(o => o.EventoData)
                .ToListAsync();
        }

        public async Task<ActionResult<Evento>> Realize(Evento evento)
        {
            evento.StatusEvento = "Realizado";
            db.Evento.Update(evento);
            await db.SaveChangesAsync();
            return evento;
        }



        public async Task<ActionResult<List<Evento>>> PendingUser(int id)
        {
            return await db.Evento
                               .Include(w => w.Categoria)
                               .Include(w => w.Comunidade)
                               .Include(w => w.Sala)
                               .Where(w => w.StatusEvento == "Pendente")
                               .Where(w => w.DeletadoEm == null)
                               .Where(w => w.Comunidade.ResponsavelUsuarioId == id)
                               .ToListAsync();
        }


        public async Task<ActionResult<List<Evento>>> RealizeUser(int id)
        {
            return await db.Evento
                              .Where(w => w.StatusEvento == "Realizado")
                              .Where(w => w.DeletadoEm == null)
                              .Where(w => w.Comunidade.ResponsavelUsuarioId == id)
                              .Include(w => w.Comunidade)
                              .Include(w => w.Categoria)
                              .Include(w => w.Sala)
                              .ToListAsync();
        }
        public async Task<ActionResult<List<Evento>>> ApprovedUser(int id)
        {
            return await db.Evento
                              .Include(w => w.Comunidade)
                              .Include(w => w.Categoria)
                              .Include(w => w.Sala)
                              .Where(w => w.StatusEvento == "Aprovado")
                              .Where(w => w.DeletadoEm == null)
                              .Where(w => w.Comunidade.ResponsavelUsuarioId == id)
                              .ToListAsync();
        }















        public async Task<ActionResult<List<Evento>>> GetEventsByUser(int id)
        {
            return await db.Evento
           .Where(w => w.DeletadoEm == null)
           .Where(w => w.StatusEvento == "aprovado")
           .Where(w => w.StatusEvento == "pendente")
           .Where(w => w.Comunidade.ResponsavelUsuarioId == id)
           .Include(i => i.Comunidade)
           .ToListAsync();
        }

        public async Task<ActionResult<Evento>> Delete(Evento evento)
        {
            evento.DeletadoEm = DateTime.Now;
            db.Entry(evento).State = EntityState.Modified;
            await db.SaveChangesAsync();
            return evento;
        }
        
        public async Task<ActionResult<Evento>> DeleteByAdministrador(Evento evento)
        {
            evento.DeletadoEm = DateTime.Now;
            db.Entry(evento).State = EntityState.Modified;
            await db.SaveChangesAsync();
            return evento;
        }

        // public async Task<ActionResult<List<Evento>>> EventByCategory(int id)
        // {
        //     var a = await db.Evento.Include(i => i.Categoria)
        //                    .Where(w => w.CategoriaId == id)
        //                    .ToListAsync();

        //     foreach (var item in a)
        //     {
        //         item.Categoria.Evento = null;
        //     }

        //     return a;
        // }

        public async Task<Evento> GetExistEvent(int id)
        {
            var evento = await db.Evento
            .Include(i => i.Categoria)
            .Include(c => c.Comunidade)
            .Where(w => w.StatusEvento != "Realizado" && w.StatusEvento != "Aprovado")
            .Where(w => w.DeletadoEm == null)
            .FirstOrDefaultAsync(f => f.EventoId == id);

            return evento;
        }

        public async Task<ActionResult<List<Evento>>> MyEventsAccept(int id)
        {
            return await db.ResponsavelEventoTw
                           .Include(w => w.EventoNavigation)
                           .ThenInclude(w => w.Comunidade)
                           .Where(w => w.ResponsavelEvento == id)
                           .Where(w => w.EventoNavigation.StatusEvento != "Recusado")
                           .Select( e => new Evento{
                               Nome = e.EventoNavigation.Nome,
                               EventoData = e.EventoNavigation.EventoData,
                               Horario = e.EventoNavigation.Horario,
                               Descricao = e.EventoNavigation.Descricao,
                               EmailContato = e.EventoNavigation.EmailContato,
                               StatusEvento = e.EventoNavigation.StatusEvento,
                               Foto = e.EventoNavigation.Foto,
                               Comunidade = e.EventoNavigation.Comunidade,
                               Categoria = e.EventoNavigation.Categoria,
                           }).ToListAsync();
        }

        public async Task<ActionResult<List<ResponsavelEventoTw>>> MyEventsReject(int id)
        {
            return await db.ResponsavelEventoTw
                              .Include(w => w.EventoNavigation)
                              .Where(w => w.EventoNavigation.StatusEvento == "Recusado")
                              .Where(w => w.ResponsavelEvento == id)
                              .ToListAsync();
        }

        public async Task<ActionResult<List<Evento>>> ExistaData(DateTime data)
        {
            var eventos = await db.Evento
           .Where(w => w.DeletadoEm == null)
           .Where(w => w.EventoData == data)
           .Include(i => i.Categoria)
           .ToListAsync();

            foreach (var item in eventos)
            {
                item.Categoria.Evento = null;
                item.Categoria.EventoTw = null;
            }

            return eventos;
        }

        private void Mensagem(string email, string status)
        {
            try
            {
                // Estancia da Classe de Mensagem
                MailMessage _mailMessage = new MailMessage();
                // Remetente
                _mailMessage.From = new MailAddress(email);

                // Destinatario seta no metodo abaixo

                //Contrói o MailMessage
                _mailMessage.CC.Add(email);
                _mailMessage.Subject = "TESTELIGHT CODE XP";
                _mailMessage.IsBodyHtml = true;
                _mailMessage.Body = "<b>Olá Tudo bem ??</b><p>Seu evento foi " + status + "</p>";

                //CONFIGURAÇÃO COM PORTA
                SmtpClient _smtpClient = new SmtpClient("smtp.gmail.com", Convert.ToInt32("587"));

                //CONFIGURAÇÃO SEM PORTA
                // SmtpClient _smtpClient = new SmtpClient(UtilRsource.ConfigSmtp);

                // Credencial para envio por SMTP Seguro (Quando o servidor exige autenticação);
                _smtpClient.UseDefaultCredentials = false;

                _smtpClient.Credentials = new NetworkCredential("communitythoughtworks@gmail.com", "tw.123456");

                _smtpClient.EnableSsl = true;

                _smtpClient.Send(_mailMessage);

            }
            catch (Exception ex)
            {
                throw ex;
            }
        }


        public async Task<ActionResult<Evento>> Update(Evento evento)
        {
            db.Entry(evento).State = EntityState.Modified;
            await db.SaveChangesAsync();
            return evento;
        }

    }
}