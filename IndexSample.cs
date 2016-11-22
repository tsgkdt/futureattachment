using Nest;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using Elasticsearch.Net;

namespace FutureOfAttachments
{
    class IndexSample
    {
        private readonly string DocumentsIndex = "documents";

        private readonly ElasticClient _client = null;

        /// <summary>
        /// ElasticClientを生成するコンストラクタ
        /// 接続したいElasticSearchのエンドポイントを指定します。(例：new Uri("http://192.168.1.1:9200/"))
        /// </summary>
        /// <param path="uri">ElasticSearch接続先</param>
        public IndexSample(Uri uri = null)
        {
            var connectionSettings = new ConnectionSettings(uri)
                .InferMappingFor<Document>(m => m
                        .IndexName(DocumentsIndex)
                );
            _client = new ElasticClient(connectionSettings);
        }

        /// <summary>
        /// Indexとマッピングを生成します。
        /// </summary>
        /// <returns></returns>
        public IndexSample CreateMapping()
        {
            /*
            var indexResponse = _client.CreateIndex(DocumentsIndex, c => c
                .Settings(s => s
                        .Analysis(a => a
                                .Analyzers(ad => ad
                                        .Custom("windows_path_hierarchy_analyzer", ca => ca
                                                .Tokenizer("windows_path_hierarchy_tokenizer")
                                        )
                                )
                                .Tokenizers(t => t
                                        .PathHierarchy("windows_path_hierarchy_tokenizer", ph => ph
                                                .Delimiter('\\')
                                        )
                                )
                        )
                )
                .Mappings(m => m
                    .Map<Document>(mp => mp
                        .AutoMap()
                        .AllField(all => all
                                .Enabled(false)
                        )
                        .Properties(ps => ps
                            .Text(s => s
                                    .Name(n => n.Path)
                                    .Analyzer("windows_path_hierarchy_analyzer")
                            )
                            .Object<Attachment>(a => a
                                .Name(n => n.Attachment)
                                .Properties(p => p
                                    .Text(t => t
                                        .Name(n => n.Content)
                                        .Fields(f => f
                                            .Keyword(k => k.IgnoreAbove(256)))
                                        )
                                    )

                            .AutoMap()
                            )
                        )
                    )
                )
            );
*/
            var indexResponse = _client.CreateIndex(DocumentsIndex, c => c
                .Settings(s => s
                    .Analysis(a => a
                        .Analyzers(ad => ad
                            .Custom("windows_path_hierarchy_analyzer", ca => ca
                                .Tokenizer("windows_path_hierarchy_tokenizer")
                            )
                        )
                        .Tokenizers(t => t
                            .PathHierarchy("windows_path_hierarchy_tokenizer", ph => ph
                                .Delimiter('\\')
                            )
                        )
                    )
                )
                .Mappings(m => m
                    .Map<Document>(mp => mp
                        .AllField(all => all
                            .Enabled(false)
                        )
                        .Properties(ps => ps
                            .Number(n => n
                                .Name(nn => nn.Id)
                            )
                            .Text(s => s
                                .Name(n => n.Path)
                                .Analyzer("windows_path_hierarchy_analyzer")
                            )
                            .Object<Attachment>(a => a
                                .Name(n => n.Attachment)
                                .Properties(p => p
                                    .Text(t => t
                                        .Name(n => n.Name)
                                    )
                                    .Text(t => t
                                        .Name(n => n.Content)
                                    )
                                    .Text(t => t
                                        .Name(n => n.ContentType)
                                    )
                                    .Number(n => n
                                        .Name(nn => nn.ContentLength)
                                    )
                                    .Date(d => d
                                        .Name(n => n.Date)
                                    )
                                    .Text(t => t
                                        .Name(n => n.Author)
                                    )
                                    .Text(t => t
                                        .Name(n => n.Title)
                                    )
                                    .Text(t => t
                                        .Name(n => n.Keywords)
                                    )
                                )
                            )
                        )
                    )
                )
            );
            return this;
    }

        /// <summary>
        /// マッピングを取得します。
        /// </summary>
        /// <returns></returns>
        public IGetMappingResponse GetMapping()
        {
            var mappingResponse = _client.GetMapping<Document>();
            return mappingResponse;
        }

        /// <summary>
        /// Attachments用のパイプラインを設定します。
        /// </summary>
        /// <returns></returns>
        public IndexSample PutPipeline()
        {
            _client.PutPipeline("attachments", p => p
                .Description("Document attachment pipeline")
                .Processors(pr => pr
                    .Attachment<Document>(a => a
                        .Field(f => f.Content)
                        .TargetField(f => f.Attachment)
                        .IndexedCharacters(-1)
                    )
                    .Remove<Document>(r => r
                        .Field(f => f.Content)
                    )
                )
            );
            return this;
        }

        /// <summary>
        /// ファイルを登録します。
        /// </summary>
        /// <param path="id">ID</param>
        /// <param path="file">インデックスしたいファイル</param>
        /// <returns></returns>
        public IndexSample AddAttachment(int id, FileInfo file)
        {
            var bytes = File.ReadAllBytes(Path.Combine(file.FullName));
            Index(bytes, id, file.FullName);
            return this;
        }

        /// <summary>
        /// ネット上に転がっているファイルを登録します。
        /// </summary>
        /// <param path="id">ID</param>
        /// <param path="uri">URL</param>
        /// <returns></returns>
        public IndexSample AddAttachment(int id, Uri uri)
        {
            var bytes = new HttpClient().GetByteArrayAsync(uri).Result;
            Index(bytes, id, uri.AbsolutePath);
            return this;
        }

        /// <summary>
        /// ElasticSearchに登録します
        /// </summary>
        /// <param path="content">ファイルの中身</param>
        /// <param path="id">登録するID</param>
        /// <param path="path">パスに指定する</param>
        private void Index(byte[] content, int id, string path)
        {
            var base64File = Convert.ToBase64String(content);
            var res = _client.Index(new Document
            {
                Id = id,
                Path = path,
                Content = base64File
            }, i => i.Pipeline("attachments"));

            Console.WriteLine(res.DebugInformation);
        }

        /// <summary>
        /// IDを指定して１件取得します
        /// </summary>
        /// <param name="id">ID</param>
        /// <returns>Document</returns>
        public Document GetDocument(int id)
        {
            var searchResponse = _client.Search<Document>(g => g
                .Index(DocumentsIndex)
                .Type("document")
                .Query(q => q.Term(qt => qt.Field("id").Value(id)))
                .Take(1));
            return searchResponse.Documents.First();
        }
    }
}
