using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Bibliotec.Contexts;
using Bibliotec.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Bibliotec_mvc.Controllers
{
    [Route("[controller]")]
    public class LivroController : Controller
    {
        private readonly ILogger<LivroController> _logger;

        public LivroController(ILogger<LivroController> logger)
        {
            _logger = logger;
        }

        Context context = new Context();
        public IActionResult Index()
        {
            ViewBag.Admin = HttpContext.Session.GetString("Admin")!;

            List<Livro> ListaLivros = context.Livro.ToList();

            var LivrosReservados = context.LivroReserva.ToDictionary(Livro => Livro.LivroID, Livror => Livror.DtReserva);

            ViewBag.Livros = ListaLivros;
            ViewBag.LivrosComReserva = LivrosReservados;

            return View();
        }

        // Metodo que retorna a tela de cadastro:
        [Route("Cadastro")]
        public IActionResult Cadastro()
        {

            ViewBag.Admin = HttpContext.Session.GetString("Admin")!;

            ViewBag.Categorias = context.Categoria.ToList();
            // retorna a view de cadastro
            return View();
        }

        // Metodo para cadastrar um livro:
        [Route("Cadastrar")]
        public IActionResult Cadastrar(IFormCollection form)

        {

            // PRIMEIRA PARTE: Cdastrar um livro na tabela livro 
            Livro novoLivro = new Livro();

            // O que meu usuario escrever no formulario sera atribuido ao novoLivro
            novoLivro.Nome = form["Nome"].ToString();
            novoLivro.Descricao = form["Descricao"].ToString();
            novoLivro.Editora = form["Editora"].ToString();
            novoLivro.Escritor = form["Escritor"].ToString();
            novoLivro.Idioma = form["Idioma"].ToString();
            // IMG
            if (form.Files.Count > 0)
            {
                // PRIMEIRO PASSO
                var arquivo = form.Files[0];

                //SEGUNDO PASSO
                var pasta = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/imagens/Livros");

                if (!Directory.Exists(pasta))
                {
                    // CRIAR A PASTA:
                    Directory.CreateDirectory(pasta);

                }
                // terceiro passo:
                //CRIAR A VARIAVEL PARA ARMAZENAR O CAMINHO EM QUE O ARQUIVO ESTARA,ALEM DO NOME DELE
                var caminho = Path.Combine(pasta, arquivo.FileName);

                using (var stream = new FileStream(caminho, FileMode.Create))
                {
                    arquivo.CopyTo(stream);

                }

                novoLivro.Imagem = arquivo.FileName;
            }else{
                novoLivro.Imagem= "padrao.png";
            }
            context.Livro.Add(novoLivro);
            context.SaveChanges();

            // SEGUNDA PARTE: E adcionar dentro de LivroCategoria a categoria que pertence ao novoLivro
            List<LivroCategoria> ListalivroCategoria = new List<LivroCategoria>(); // LISTA AS CATEGORIAS

            //Array que possui as categorias selecionadas pelo usuario
            string[] categoriasSelecionadas = form["Categoria"].ToString().Split(',');
            // Acao, Terro, Suspense
            // 3, 5, 7

            foreach (string categoria in categoriasSelecionadas)
            {
                //categoria possui a informacao do id da categoria ATUAL selecionada.
                LivroCategoria LivroCategoria = new LivroCategoria();

                LivroCategoria.CategoriaID = int.Parse(categoria);
                LivroCategoria.LivroID = novoLivro.LivroID;

                ListalivroCategoria.Add(LivroCategoria);

            }

            context.LivroCategoria.AddRange(ListalivroCategoria);

            context.SaveChanges();

            return LocalRedirect("Livro/Cadastro");





            // [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
            // public IActionResult Error()
            // {
            //     return View("Error!");
            // }
        }

        [Route("Editar/{id}")]
        public IActionResult Editar(int id){

             ViewBag.Admin = HttpContext.Session.GetString("Admin")!;

             ViewBag.CategoriasDoSistema = context.Categoria.ToList();

             // LivroID = 3

             //Buscar quem e o tal do id numero 3:
             Livro Livro = context.Livro.FirstOrDefault(LivroCategoria => LivroCategoria.LivroID == id)!;

             //Buscar as categorias que o livro possui
             var categoriaDoLivro = context.LivroCategoria.Where(indentificadorLivro => indentificadorLivro.LivroID == id).Select(Livro => Livro.Categoria).ToList();

             //Quero pegar as infromacoes do meu livro selecionado e mandar para a minha View
             ViewBag.Livro = Livro;
             ViewBag.Categoria = categoriaDoLivro;
             
            return View();
        }

        // Metodo que atualiza as informacoes do livro
        [Route("Atualizar")]
        public IActionResult Atualizar(IFormCollection form, int id, IFormFile imagem){
            //Buscar um livro especifico pelo ID
            Livro livroAtualizado = context.Livro.FirstOrDefault(livro => livro.LivroID == id)!;

            livroAtualizado.Nome = form["Nome"];
            livroAtualizado.Escritor = form["Escritor"];
            livroAtualizado.Editora = form["Editora"];
            livroAtualizado.Idioma = form["Idioma"];
            livroAtualizado.Descricao = form["Descricao"];

            //Uploud da imagem
            if(imagem ! = null && imagem.Length > 0){
                //Definir o caminho da minha imagem:
                var caminhoImagem = Path.Combine("wwwroot/images/Livros", imagem.FileName);

                //Verificar se minha imagem ainda existe no meu caminho
                //Caso exista, ela ira apagada
                if(!string.IsNullOrEmpty(livroAtualizado.Imagem)){
                    //Caso exista, ela ira ser apagada
                var caminhoImagemAntiga = Path.Combine("wwwroot/images/Livros", livroAtualizado.Imagem);
                //  Ver se existe uma imagem no caminho antigo
                if(System.IO.File.Exists(caminhoImagemAntiga)){
                    System.IO.File.Delete(caminhoImagemAntiga);
                }

                }
                //Salvar a imagem nova
                using(var stream = new FileStream(caminhoImagem, FileMode.Create)){
                    imagem.CopyTo(stream);
                }

                //Subir essa mudanca para o meu banco de dados
                livroAtualizado.Imagem = imagem.FileName;

                }

                //CATEGORIAS:

                //PRIMEIRO: Prescisamos pegar as categorias selecionadas do usuario
                var categoriasSelecionadas = form["Categoria"].ToList();
                //SEGUNDO: Pegaremos as categorias ATUAIS do livro
                var categoriasAtuais = context.LivroCategoria.Where(livro => livro.LivroID == id).ToList();
                //TERCEIRO: Removeremos as categorias antigas
                foreach(var categoria in categoriasAtuais){
                    if(!categoriasSelecionadas.Contains(categoria.CategoriaID.ToString())){
                        //Nos vamos remover a categoria do nosso context
                        context.LivroCategoria.Remove(categoria);
                    }

                }
                //QUARTO: Adcionaremos as novas categorias
                foreach(var categoria in categoriasSelecionadas){
                    if(!categoriasAtuais.Any(c => c.CategoriaID.ToString() == categoria)){
                    context.LivroCategoria.Add(new LivroCategoria{
                        LivroID  = id,
                        CategoriaID = int.Parse(categoria)
                    });

                    }

                }

                context.SaveChanges();

                return LocalRedirect("/Livro");
                

            }

            //Metodo de excluir o livro
            [Route("Excluir")]
            public IActionResult Excluir(){
                // Buscar qual o livro do id que prescisamos excluir
                Livro livroEncontrado = context.Livro.First(Livro => Livro.LivroID == id);

                //Buscar as categorias desse livro:
                var categoriasDoLivro = context.LivroCategoria.Where(Livro => Livro.LivroID == id).ToList();

                //Prescisa excluir primeiro o registro da tabela intermediaria
                foreach(var categoria in categoriasDoLivro){
                    context.LivroCategoria.Remove(categoria);
                }

                context.Livro.Remove(livroEncontrado);

                context.SaveChanges();


                return LocalRedirect("/Livro");
            }


            // [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
            // public IActionResult Error()
            // {
            //     return View("Error!");
            // }





            }







        }


