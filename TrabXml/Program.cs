using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml;
using System.Xml.Xsl;
using System.Xml.Schema;
using System.IO;
using Saxon.Api;

namespace TrabXml
{
    class Program
    {

        static string pathXtm, pathXsd, pathDtd, pathXsl;

        static void Main(string[] args)
        {
            pathXtm = Directory.GetCurrentDirectory() + "\\GioMovies.xtm";
            pathXsd = Directory.GetCurrentDirectory() + "\\GioMovies.xsd";
            pathDtd = Directory.GetCurrentDirectory() + "\\GioMovies.dtd";
            pathXsl = Directory.GetCurrentDirectory() + "\\Transformacao.xsl";
            //ValidaDtd();
            //ValidaSchema();
            //Consulta();
            Transforma();
        }

        static void Transforma()
        {
            /* cria uma instancia do processador */
            Processor processor = new Processor();

            /* carrega o documento a ser processado */
            XdmNode input = processor.NewDocumentBuilder().Build(new Uri(pathXtm));

            /* cria o transformer com o stylesheet informado */
            Xslt30Transformer transformer = processor.NewXsltCompiler().Compile(new Uri(pathXsl)).Load30();
            transformer.BaseOutputURI = "file:///" + Directory.GetCurrentDirectory().Replace("\\", "/");
            //String outfile = Directory.GetCurrentDirectory() + "\\Saida";
            Serializer serializer = processor.NewSerializer();
            //serializer.SetOutputStream(new FileStream(outfile, FileMode.Create, FileAccess.Write));

            transformer.ApplyTemplates(input, serializer);
        }

        static void ValidaDtd()
        {
            XmlReaderSettings settings = new XmlReaderSettings();
            /* define o tipo de validação */
            settings.ValidationType = ValidationType.DTD;
            settings.DtdProcessing = DtdProcessing.Parse;
            // settings.IgnoreWhitespace = true;
            /* usado para acessar documentos externos(arquivo dtd) */
            settings.XmlResolver = new XmlUrlResolver();
            /* metodo que sera invocado toda vez que um evento de validacao ocorrer */
            settings.ValidationEventHandler += EventHandler;
            XDocument doc = XDocument.Load(pathXtm);
            Stream buffer;
            XmlReader reader;
            /* se o documento nao contem doctype, associa o dtd */
            if (doc.DocumentType == null)
            {
                buffer = AssignDtd(doc, pathDtd);
                reader = XmlReader.Create(buffer, settings);
            }
            else
                reader = XmlReader.Create(pathXtm, settings);
            try
            {
                /* faz a leitura de todos os dados XML */
                while (reader.Read())
                {
                }
            }
            catch (XmlException e)
            {
                /* ocorre se o documento XML inclui caracteres ilegais ou tags que não aninhadas corretamente */
                Console.WriteLine("Erro durante a iteracao:");
                Console.WriteLine(e.Message);
            }
            finally
            {
                reader.Close();
            }
        }

        static void ValidaSchema()
        {
            /* define o tipo de validacao */
            XmlReaderSettings settings = new XmlReaderSettings();
            settings.ValidationType = ValidationType.Schema;
            /* carrega o arquivo de esquema */
            XmlSchemaSet schemas = new XmlSchemaSet();
            settings.Schemas = schemas;
            /* ao dicionar o eschema, especifica-se o namespace que ele valida e sua localizacao */
            schemas.Add(null, pathXsd);
            settings.ValidationEventHandler += EventHandler;
            settings.DtdProcessing = DtdProcessing.Ignore;
            /* especifica o tratamento de evento para os erros de validacao */
            settings.ValidationType = ValidationType.Schema;
            settings.ValidationFlags |= XmlSchemaValidationFlags.ProcessInlineSchema;
            settings.ValidationFlags |= XmlSchemaValidationFlags.ProcessSchemaLocation;
            settings.ValidationFlags |= XmlSchemaValidationFlags.ReportValidationWarnings;

            XmlReader reader = XmlReader.Create(pathXtm, settings);
            try
            {
                while (reader.Read())
                {
                }
            }
            catch (XmlException e)
            {
                Console.WriteLine("Erro durante a iteracao:");
                Console.WriteLine(e.Message);
            }
            finally
            {
                reader.Close();
            }
        }

        static void EventHandler(object sender, ValidationEventArgs e)
        {
            Console.WriteLine(e.Message);
        }

        static void Consulta()
        {
            XElement root = XElement.Load(pathXtm);

            /* seleciona os nomes e os ids dos generos */
            var gen = (from g in root.Descendants("topicRef")
                       where g.Attribute("href").Value == "#Genero"
                       select new
                       {
                           nomeGenero = (string)g.Parent.Parent.Element("baseName").Element("baseNameString"),
                           id = (string)g.Parent.Parent.Element("instanceOf").Element("topicRef").Attribute("href")
                       }).ToList();

            /* busca o id do ano 2000 buscando por topicos com baseNameString = 2000 e instanceof.topicref.href = #ano */
            var id2000 = (root.Elements("topic").Select(x => new
            {
                id = x.Attribute("id").Value,
                ano = x.Element("baseName").Element("baseNameString").Value,
                tipo = x.Element("instanceOf").Element("topicRef").Attribute("href").Value
            }).FirstOrDefault(x => x.ano == "2000" && x.tipo == "#Ano")).id;

            /* todas as associacoes com o id do ano 2000 */
            var associacoes2000 = (root.Elements("association").Select(x => new
            {
                tipo = (string)x.Element("instanceOf").Element("topicRef").Attribute("href").Value,
                filme = (string)x.Element("member").Element("topicRef").Attribute("href"),
                idAno = (string)x.Elements().Last().Element("topicRef").Attribute("href")
            }).Where(x => x.idAno == "#" + id2000 && x.tipo == "#filme-ano")).ToList();

            /* lista os nomes de todos os filmes */
            var filmes = root.Elements("topic").Select(x => new {
                nome = (string)x.Element("baseName").Element("baseNameString"),
                id = (string)x.Attribute("id"),
                tipo = (string)x.Element("instanceOf")?.Element("topicRef").Attribute("href"),
            }).Where(x => x.tipo == "#Filme").ToList();

            /* join entre os filmes e as associacoes encontradas anteriormente */
            var nomes_filmes_2000 = (from f in filmes join i in associacoes2000 on "#" + f.id equals i.filme orderby f.nome select f.nome).ToList();

            /* Pega o id da sinopse buscando por topicos com baseNameString = sinopse */
            var idSinopse = (root.Elements("topic").Select(x => new
            {
                id = x.Attribute("id").Value,
                tipo = x.Element("baseName").Element("baseNameString").Value,
            }).FirstOrDefault(x => x.tipo == "sinopse")).id;

            /* identifica os filmes que contem a palavra 'especial' na sinopse a partir do id encontrado anteriormente  */
            var filmesComEspecialNaSinopse = (root.Descendants("occurrence").Select(x => new
            {
                tipo = (string)x.Element("scope")?.Element("topicRef").Attribute("href"),
                sinopse = (string)x?.Element("resourceData"),
                idIngles = (string)x.Parent.Element("occurrence").Element("scope").Element("topicRef").Attribute("href"),
                nomeIngles = (string)x.Parent.Element("occurrence").Element("scope").Element("topicRef").Attribute("href").Parent.Parent.Parent.Element("resourceData")
            })).Where(x => x.tipo == "#sinopse" && x.sinopse.Contains("especial") && x.idIngles == "#ingles").ToList();

            /* busca o id do genero thriller buscando topicos com baseNameString = thriller */
            var idThriller = (root.Elements("topic").Select(x => new
            {
                id = (string)x.Attribute("id"),
                genero = (string)x.Element("baseName")?.Element("baseNameString"),
                tipo = (string)x.Element("instanceOf")?.Element("topicRef").Attribute("href")
            }).FirstOrDefault(x => x.genero == "Thriller" && x.tipo == "#Genero")).id;

            /* todas as associacoes filme-genero com genero = thriller */
            var associacoesFilmeThriller = (root.Elements("association").Select(x => new
            {
                tipo = (string)x.Element("instanceOf").Element("topicRef").Attribute("href").Value,
                idFilme = (string)x.Element("member").Element("topicRef").Attribute("href"),
                genero = (string)x.Elements().Last().Element("topicRef").Attribute("href")
            }).Where(x => x.genero == "#" + idThriller && x.tipo == "#filme-genero")).ToList();

            /* todos os sites */
            var sitesDosFilmes = (root.Descendants("occurrence").Select(x => new
            {
                tipo = (string)x.Element("instanceOf")?.Element("topicRef")?.Attribute("href"),
                site = (string)x.Element("resourceRef")?.Attribute("href"),
                idFilme = (string)x.Parent.Attribute("id")
            })).Where(x => x.tipo == "#site").ToList();

            /* join entre sites e as associacoes */
            var sites_filmes_thriller = (from s in sitesDosFilmes join a in associacoesFilmeThriller on "#" + s.idFilme equals a.idFilme select a).ToList();

            /* filmes com mais de três atores no elenco de apoio */
            var fives = root.Elements("topic").Select(x => new {
                nome = (string)x.Element("baseName").Element("baseNameString"),
                countElencoApoio = x.Descendants("topicRef").Count(e => e.Attribute("href")?.Value == "#elencoApoio"),
                tipo = (string)x.Element("instanceOf")?.Element("topicRef").Attribute("href")
            }).Where(x => x.countElencoApoio > 3 && x.tipo == "#Filme").ToList();

            /* todas as associacoes filme-elenco */
            var associacoesFilmeElenco = (root.Elements("association").Select(x => new
            {
                tipo = (string)x.Element("instanceOf").Element("topicRef").Attribute("href").Value,
                idFilme = (string)x.Element("member").Element("topicRef").Attribute("href"),
                elenco = (string)x.Elements().Last().Element("topicRef").Attribute("href")
            }).Where(x => x.tipo == "#filme-elenco")).ToList();

            /* lista os nomes de todos os atores */
            var elenco = root.Elements("topic").Select(x => new {
                nome = (string)x.Element("baseName").Element("baseNameString"),
                id = (string)x.Attribute("id"),
                tipo = (string)x.Element("instanceOf")?.Element("topicRef").Attribute("href")
            }).Where(x => x.tipo == "#Elenco").ToList();


            /* busca os filmes com sinopse */
            var filmesComSinopse = (root.Descendants("occurrence").Select(x => new
            {
                tipo = (string)x.Element("scope")?.Element("topicRef").Attribute("href"),
                sinopse = (string)x?.Element("resourceData"),
                id = (string)x.Parent.Element("occurrence").Element("scope").Element("topicRef").Attribute("href").Parent.Parent.Parent.Parent.Attribute("id")
            })).Where(x => x.tipo == "#sinopse").ToList();

            /* join entre sites e as associacoes filme-elenco */
            var filmes_sinopse_elenco = (from f in filmesComSinopse join a in associacoesFilmeElenco on "#" + f.id equals a.idFilme join e in elenco on a.elenco equals "#" + e.id where f.sinopse.Contains(e.nome) select f.id).Distinct().ToList();

        }

        /* associa dtd ao arquivo e retorna stream desse arquivo */
        static Stream AssignDtd(XDocument doc, string pathDtd)
        {
            Stream stream = new MemoryStream();
            if (doc.DocumentType != null)
                doc.DocumentType.Remove();
            var doctype = new XDocumentType(doc.Root.Name.LocalName, null, pathDtd, null);
            doc.AddFirst(doctype);
            doc.Save(stream);
            stream.Position = 0;
            return stream;
        }

    }
}
