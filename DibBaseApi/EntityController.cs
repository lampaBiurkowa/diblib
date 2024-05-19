using DibBase.Extensions;
using DibBase.Infrastructure;
using DibBase.ModelBase;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DibBaseSampleApi.Controllers;
[ApiController]
[Route("[controller]")]
public class EntityController<T>(Repository<T> repository) : ControllerBase where T : Entity
{
    protected T HidePrivateId(T entity)
    {
        entity.Id = default;
        return entity;
    }

    [HttpGet]
    public virtual async Task<IActionResult> Get(int skip = 0, int take = 1000, CancellationToken ct = default)
    {
        var entities = (await repository.GetAll(skip, take).ToListAsync(ct)).Select(HidePrivateId);
        return entities != null ? Ok(entities) : NotFound();
    }

    [HttpGet("{id}")]
    public virtual async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        var entity = await repository.GetById(id.Deobfuscate().Id, ct);
        return entity != null ? Ok(HidePrivateId(entity)) : NotFound();
    }

    [HttpPost("ids")]
    public virtual async Task<IActionResult> Get(List<Guid> ids, CancellationToken ct) => Ok((await repository.GetByIds(ids.Select(x => x.Deobfuscate().Id), ct)).Select(HidePrivateId));

    [HttpPost]
    public virtual async Task<IActionResult> Add(T entity, CancellationToken ct)
    {
        await repository.InsertAsync(entity, ct);
        await repository.CommitAsync(ct);
        return Ok(entity.Guid);
    }

    [HttpPut("{id}")]
    public virtual async Task<IActionResult> Update(Guid id, T entity, CancellationToken ct)
    {
        entity.Id = id.Deobfuscate().Id;
        await repository.UpdateAsync(entity, ct);
        await repository.CommitAsync(ct);
        return Ok(entity.Guid);
    }

    [HttpDelete("{id}")]
    public virtual async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await repository.DeleteAsync(id.Deobfuscate().Id, ct);
        await repository.CommitAsync(ct);
        return Ok();
    }
}
