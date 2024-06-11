using DibBase.Extensions;
using DibBase.Infrastructure;
using DibBase.ModelBase;
using DibBaseApi;
using Microsoft.AspNetCore.Mvc;

namespace DibBaseSampleApi.Controllers;
[ApiController]
[Route("[controller]")]
public class EntityController<T>(Repository<T> repository) : ControllerBase where T : Entity
{
    protected Repository<T> repo = repository;

    [HttpGet]
    public virtual async Task<ActionResult<List<T>>> Get(int skip = 0, int take = 1000, CancellationToken ct = default)
    {
        var entities = (await repo.GetAll(skip, take, ct: ct)).Select(IdHelper.HidePrivateId);
        return entities != null ? Ok(entities) : NotFound();
    }

    [HttpGet("{id}")]
    public virtual async Task<ActionResult<T>> Get(Guid id, CancellationToken ct)
    {
        var entity = await repo.GetById(id.Deobfuscate().Id, ct: ct);
        return entity != null ? Ok(IdHelper.HidePrivateId(entity)) : NotFound();
    }

    [HttpPost("ids")]
    public virtual async Task<ActionResult<List<T>>> Get(List<Guid> ids, CancellationToken ct) =>
        Ok((await repo.GetByIds(ids.Select(x => x.Deobfuscate().Id), ct: ct)).Select(IdHelper.HidePrivateId));

    [HttpPost]
    public virtual async Task<ActionResult<Guid>> Add(T entity, CancellationToken ct)
    {
        await repo.InsertAsync(entity, ct);
        await repo.CommitAsync(ct);
        return Ok(entity.Obfuscate());
    }

    [HttpPut]
    public virtual async Task<ActionResult<Guid>> Update(T entity, CancellationToken ct)
    {
        entity.Id = entity.Guid.Deobfuscate().Id;
        await repo.UpdateAsync(entity, ct);
        await repo.CommitAsync(ct);
        return Ok(entity.Guid);
    }

    [HttpDelete("{id}")]
    public virtual async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await repo.DeleteAsync(id.Deobfuscate().Id, ct);
        await repo.CommitAsync(ct);
        return Ok();
    }
}
