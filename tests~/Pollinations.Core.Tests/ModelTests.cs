using System.Collections.Generic;

namespace Pollinations.Tests
{
    public static class ModelTests
    {
        private const string Catalogue = @"[
          {""name"":""nova-fast"",""title"":""Amazon Nova Micro"",""category"":""text"",""publisher"":""amazon"",
           ""aliases"":[""nova-micro""],""supported_endpoints"":[""/v1/chat/completions"",""/text/{prompt}""]},
          {""name"":""tongyi-mai/z-image-turbo"",""category"":""image"",""supported_endpoints"":[""/image/{prompt}""]},
          {""name"":""openai/gpt-transcribe"",""category"":""audio"",""supported_endpoints"":[""/v1/audio/transcriptions""]},
          {""title"":""no id at all""}
        ]";

        public static void Run()
        {
            Check.Suite("models");

            List<PollinationsModel> models = PollinationsModels.Parse(Catalogue);
            Check.Equal(3, models.Count, "skips an entry without an id");
            Check.Equal("nova-fast", models[0].Id, "reads the name field");
            Check.Equal("Amazon Nova Micro", models[0].Title, "reads the title");
            Check.Equal("Amazon Nova Micro", models[0].Label, "label prefers the title");
            Check.Equal("image", models[1].Category, "reads the category");
            Check.Equal(1, models[0].Aliases.Count, "reads aliases");
            Check.Equal("nova-micro", models[0].Aliases[0], "alias value");
            Check.Equal(2, models[0].SupportedEndpoints.Count, "reads supported endpoints");

            Check.True(models[0].Supports("/v1/chat/completions"), "supports chat");
            Check.False(models[1].Supports("/v1/chat/completions"), "the image model does not support chat");
            Check.True(models[1].Supports(""), "an empty endpoint matches everything");

            Check.Equal(1, PollinationsModels.Supporting(models, "/v1/chat/completions").Count, "filters by endpoint");
            Check.Equal(1, PollinationsModels.Supporting(models, "/image/{prompt}").Count, "filters images by endpoint");
            Check.Equal(0, PollinationsModels.Supporting(models, "/v1/audio/speech").Count, "the transcription model is not usable for speech");
            Check.Equal(3, PollinationsModels.Supporting(models, "").Count, "no filter keeps everything");

            Check.Equal(new List<string> { "nova-fast", "tongyi-mai/z-image-turbo", "openai/gpt-transcribe" }, PollinationsModels.Ids(models), "ids in order");
            Check.True(PollinationsModels.HasModel(models, "nova-fast"), "finds a model by id");
            Check.True(PollinationsModels.HasModel(models, "nova-micro"), "finds a model by alias");
            Check.False(PollinationsModels.HasModel(models, "nope"), "does not invent a model");
            Check.False(PollinationsModels.HasModel(models, ""), "an empty id is never a model");
            Check.Equal("nova-fast", PollinationsModels.Find(models, "nova-micro").Id, "find resolves an alias");
            Check.Null(PollinationsModels.Find(models, "nope"), "find returns null when missing");

            Check.Equal(0, PollinationsModels.Parse("").Count, "empty text gives no models");
            Check.Equal(0, PollinationsModels.Parse("not json").Count, "garbage gives no models");
            Check.Equal(0, PollinationsModels.Parse("{\"unexpected\":true}").Count, "an object without a list gives no models");

            List<PollinationsModel> enveloped = PollinationsModels.Parse("{\"data\":[{\"name\":\"a\"},{\"name\":\"b\"}]}");
            Check.Equal(2, enveloped.Count, "reads an enveloped list");
            Check.Equal("a", enveloped[0].Id, "enveloped id");

            List<PollinationsModel> withId = PollinationsModels.Parse("[{\"id\":\"legacy\"}]");
            Check.Equal("legacy", withId[0].Id, "accepts an id field");
            Check.Equal("legacy", withId[0].Label, "label falls back to the id");
        }
    }
}
