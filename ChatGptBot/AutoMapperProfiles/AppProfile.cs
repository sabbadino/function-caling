using AutoMapper;
using ChatGptBot.Chain.Dto;
using ChatGptBot.Dtos.Completion.Controllers;
using ChatGptBot.Repositories.Entities;
using ChatGptBot.Services;

namespace ChatGptBot.AutoMapperProfiles
{
    public class AppProfile : Profile
    {
        public AppProfile()
        {
            CreateMap<EmbeddingForDb, Embedding>();

            CreateMap<Answer, AnswerToUserDto>().ForMember(dest =>
                    dest.Answer,
                opt => opt.MapFrom(src => !string.IsNullOrEmpty(src.TranslatedAnswerFromAi)? src.TranslatedAnswerFromAi: src.AnswerFromChatGpt));
        }
    }
}
